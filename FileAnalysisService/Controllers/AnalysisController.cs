using FileAnalysisService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalysisService.Controllers;

/// <summary>
/// Контроллер для управления анализом текстовых файлов.
/// </summary>
[ApiController]
[Route("files/analysis")]
public class AnalysisController(AnalysisService svc) : ControllerBase
{
    /// <summary>
    /// Запускает анализ файла: подсчёт слов, символов и абзацев.
    /// </summary>
    /// <param name="id">Идентификатор файла для анализа.</param>
    /// <returns>Результаты анализа или сообщение об ошибке.</returns>
    [HttpPost("{id:guid}/start")]
    public virtual async Task<IActionResult> Start(Guid id)
    {
        try
        {
            var result = await svc.AnalyzeAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка анализа файла {id}: {ex.Message}\n{ex}");
            return Problem(detail: ex.ToString(), statusCode: 500);
        }
    }

    /// <summary>
    /// Получает результаты анализа, если он уже был выполнен.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <returns>Результаты анализа или сообщение об ошибке.</returns>
    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var result = await svc.AnalyzeAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.ToString(), statusCode: 500);
        }
    }

    /// <summary>
    /// Возвращает PNG-изображение облака слов на основе текста файла.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <returns>Файл PNG или сообщение об ошибке.</returns>
    [HttpGet("{id:guid}/wordcloud")]
    public virtual async Task<IActionResult> Cloud(Guid id)
    {
        try
        {
            var (png, name) = await svc.EnsureWordCloudAsync(id);
            return File(png, "image/png", name);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[WARN] Ошибка логики: {ex.Message}");
            return Problem(statusCode: 400, detail: ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка при генерации облака: {ex}");
            return Problem(statusCode: 500, detail: ex.ToString());
        }
    }

}