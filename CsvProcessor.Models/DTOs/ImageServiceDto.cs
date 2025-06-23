using System.Collections.Concurrent;

namespace CsvProcessor.Models.DTOs;

public class ImageServiceDto
{
    public ConcurrentBag<ProductImageDto> ImageList { get; set; } = new();
    public HashSet<string> ErrorMessageList { get; set; } = new();
}
