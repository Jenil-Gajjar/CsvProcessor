using CsvProcessor.Models.DTOs;

namespace CsvProcessor.DAL.Interface;

public interface IProductRepository
{
    public Task<int> UpsertProductAsync(IDictionary<string, object> dict);

    public Task<Dictionary<string, int>> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records);

}
