namespace CsvProcessor.Models.DTOs;

public class ImageProcessDto
{
    public string ImagePath { get; set; } = string.Empty;

    public byte[]? ImageBytes { get; set; }
}
