namespace CsvProcessor.Models.DTOs;
public class ImportSummaryDto
{
    public int RowCount { get; set; }

    public int InsertedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
}
