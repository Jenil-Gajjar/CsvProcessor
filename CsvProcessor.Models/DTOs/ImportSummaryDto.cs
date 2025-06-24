namespace CsvProcessor.Models.DTOs;
public class ImportSummaryDto
{
    public int RowCount { get; set; }
    public int InsertedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int TotalSuccessfullUrls { get; set; }
    public Dictionary<string, List<string>> Information = new()
    {
        { "Category", new List<string>() },
        { "Brand", new List<string>() },
    };
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int UpdatedInventoryCount { get; set; }
}
