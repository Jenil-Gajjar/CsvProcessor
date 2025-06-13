namespace CsvProcessor.Models.DTOs;

public class ProductImageDto
{
    public int product_id { get; set; }
    public string image_path { get; set; } = string.Empty;
    public bool is_primary { get; set; }

}
