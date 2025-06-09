namespace CsvProcessor.Models.DTOs;
public class ImportSummaryDto
{
    public int SuccessCount { get; set; }
    public List<string> Messages { get; set; } = new();
}
