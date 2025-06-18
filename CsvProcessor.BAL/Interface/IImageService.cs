using System.Collections.Concurrent;
using CsvProcessor.Models.DTOs;

namespace CsvProcessor.BAL.Interface;

public interface IImageService
{

    public Task<(ConcurrentBag<ProductImageDto>, ConcurrentDictionary<string, HashSet<string>>, int)> ProcessImagesAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict);
}
