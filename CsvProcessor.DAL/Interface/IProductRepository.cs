using CsvProcessor.Models.DTOs;

namespace CsvProcessor.DAL.Interface;

public interface IProductRepository
{
    public Task<ProductDto> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records);

}
