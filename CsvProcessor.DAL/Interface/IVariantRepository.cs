namespace CsvProcessor.DAL.Interface;

public interface IVariantRepository
{
    public Task BulkInsertVariantAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict
    );
}
