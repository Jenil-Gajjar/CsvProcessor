namespace CsvProcessor.DAL.Interface;

public interface IInventoryRepository
{

    public Task<int> BulkInsertInventoryAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict);
}
