namespace FileAnalysisService.Models;

/// <summary>
/// Ответ от File Storing при загрузке PNG.
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// Уникальный идентификатор загруженного файла.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Имя загруженного файла.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Путь или URL, по которому доступен файл.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время загрузки файла.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Признак того, что файл уже существовал (дубликат).
    /// </summary>
    public bool IsDuplicate { get; set; }
}