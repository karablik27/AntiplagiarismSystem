using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FileAnalysisService.Data;
using FileAnalysisService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Services;

/// <summary>
/// Сервис, выполняющий статистический анализ файлов
/// и генерацию PNG-изображения облака слов.
/// </summary>
public class AnalysisService
{
    private readonly IHttpClientFactory _clients;
    private readonly AnalysisDbContext _db;

    /// <summary>Конструктор.</summary>
    public AnalysisService(IHttpClientFactory clients, AnalysisDbContext db)
    {
        _clients = clients;
        _db = db;
    }

    /// <summary>
    /// Анализирует файл: подсчитывает абзацы, слова, символы.  
    /// При повторном вызове возвращает кэш.
    /// </summary>
    public virtual async Task<AnalysisResult> AnalyzeAsync(Guid fileId)
    {
        Console.WriteLine($"[DEBUG] Анализ файла {fileId}");

        // 1. Попытка найти в кэше
        var cached = await _db.AnalysisResults.FindAsync(fileId);
        if (cached is not null)
        {
            Console.WriteLine("[DEBUG] Кэш найден");
            return cached;
        }

        // 2. Попытка загрузки файла
        var fs = _clients.CreateClient("FS");

        Console.WriteLine("[DEBUG] Запрашиваем файл у FileStoringService...");
        var fileBytes = await fs.GetByteArrayAsync($"/files/file/{fileId}");
        Console.WriteLine("[DEBUG] Файл получен, длина: " + fileBytes.Length);

        var text = Encoding.UTF8.GetString(fileBytes);

        Console.WriteLine("[DEBUG] Считаем хеш...");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

        if (await _db.AnalysisResults.AnyAsync(r => r.FileHash == hash && r.FileId != fileId))
            throw new InvalidOperationException("100 % совпадение с другим файлом.");

        Console.WriteLine("[DEBUG] Подсчитываем статистику...");

        var result = new AnalysisResult
        {
            FileId = fileId,
            FileHash = hash,
            Paragraphs = Regex.Split(text.Trim(), @"\r?\n\s*\r?\n").Length,
            Words = Regex.Matches(text, @"\b\w+\b").Count,
            Characters = text.Length
        };

        _db.AnalysisResults.Add(result);
        await _db.SaveChangesAsync();

        Console.WriteLine("[DEBUG] Анализ завершен");

        return result;
    }


    /// <summary>
    /// Возвращает PNG-изображение облака слов, кэшируя его в File Storing.
    /// </summary>
    public virtual async Task<(byte[] Content, string FileName)> EnsureWordCloudAsync(Guid fileId)
    {
        var res = await _db.AnalysisResults.FindAsync(fileId)
                  ?? throw new InvalidOperationException("Сначала выполните анализ файла.");

        // Картинка уже загружена.
        if (res.WordCloudFileId is not null)
        {
            var fs = _clients.CreateClient("FS");
            var png = await fs.GetByteArrayAsync($"/files/file/{res.WordCloudFileId}");
            return (png, $"{fileId}.png");
        }

        // Получаем текст файла.
        var fsClient = _clients.CreateClient("FS");
        var text = Encoding.UTF8.GetString(await fsClient.GetByteArrayAsync($"/files/file/{fileId}"));

        // Генерируем картинку через QuickChart.
        var pngBytes = await BuildWordCloudAsync(text);

        // Загружаем картинку в File Storing.
        var wcId = await UploadCloudToFsAsync(pngBytes, fileId);
        res.WordCloudFileId = wcId;
        await _db.SaveChangesAsync();

        return (pngBytes, $"{fileId}.png");
    }

    /// <summary>Формирует правильный запрос к QuickChart и возвращает PNG.</summary>
    protected virtual async Task<byte[]> BuildWordCloudAsync(string text)
    {
        var words = Regex
            .Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}\s]+", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var csv = string.Join(',', words.Select(Uri.EscapeDataString));

        var wc = _clients.CreateClient("WC");
        var url = "/wordcloud" +
                  $"?text={csv}" +
                  "&useWordList=true" +
                  "&removeStopwords=true" +
                  "&format=png&width=600&height=600";

        return await wc.GetByteArrayAsync(url);
    }

    /// <summary>Загружает PNG в File Storing и возвращает новый Guid файла.</summary>
    private async Task<Guid> UploadCloudToFsAsync(byte[] png, Guid fileId)
    {
        var fs = _clients.CreateClient("FS");

        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(png);
        content.Headers.ContentType = new("image/png");

        form.Add(content, "file", $"{fileId}.png");

        var resp = await fs.PostAsync("/files/store", form);

        if (!resp.IsSuccessStatusCode)
        {
            var msg = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Ошибка загрузки PNG: {(int)resp.StatusCode} {msg}");
        }

        var dto = await resp.Content.ReadFromJsonAsync<UploadResponse>()
                  ?? throw new InvalidOperationException("FileStoring вернул пустой ответ.");

        return dto.Id;
    }

}