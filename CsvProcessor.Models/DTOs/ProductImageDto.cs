namespace CsvProcessor.Models.DTOs;

public class ProductImageDto
{
    public int Productid { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
