namespace CsvProcessor.DAL.Interface;

public interface IInventoryRepository
{
    public Task SyncInventoryAsync(IDictionary<string, object> dict, int productid);

    public Task BulkInsertInventoryAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict);
}
