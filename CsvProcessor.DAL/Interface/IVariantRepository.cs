namespace CsvProcessor.DAL.Interface;

public interface IVariantRepository
{
    public Task SyncVariantAsync(IDictionary<string, object> dict, int productid);

    public Task BulkInsertVariantAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict
    );
}
