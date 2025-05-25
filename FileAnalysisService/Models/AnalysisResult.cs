using System.ComponentModel.DataAnnotations;

namespace FileAnalysisService.Models;

/// <summary>
/// Содержит результаты анализа конкретного текстового файла.
/// </summary>
public class AnalysisResult
{
    [Key]
    public Guid FileId { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public int Paragraphs { get; set; }
    public int Words { get; set; }
    public int Characters { get; set; }
    public Guid? WordCloudFileId { get; set; }
}