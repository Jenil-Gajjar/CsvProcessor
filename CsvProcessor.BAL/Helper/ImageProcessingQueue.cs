using System.Collections.Concurrent;
using CsvProcessor.Models.DTOs;
namespace CsvProcessor.BAL.Helper;
public static class ImageProcessingQueue
{
    public static readonly ConcurrentQueue<ImageProcessDto> ImageQueue = new();
    
}
