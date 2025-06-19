namespace CsvProcessor.Models.DTOs;

public class ProductDto
{
    public Dictionary<string, int>? SkuToIdDict { get; set; }

    public int InsertedRecords { get; set; }
    public int UpdatedRecords { get; set; }
}
