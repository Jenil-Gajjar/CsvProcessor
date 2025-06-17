namespace CsvProcessor.Models.DTOs;

public class ImageProcessDto
{
    public string ImagePath { get; set; } = string.Empty;
    public HttpContent? ResponseContent { get; set; }
}
