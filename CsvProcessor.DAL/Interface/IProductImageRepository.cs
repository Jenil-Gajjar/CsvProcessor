using System.Collections.Concurrent;
using CsvProcessor.Models.DTOs;

namespace CsvProcessor.DAL.Interface;

public interface IProductImageRepository
{
    public Task BulkInsertImagesAsync(ConcurrentBag<ProductImageDto>? set);

}
