namespace CsvProcessor.DAL.Interface;

public interface IProductRepository
{

    public Task<(Dictionary<string, int>, Dictionary<string, int>)> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records);

}
