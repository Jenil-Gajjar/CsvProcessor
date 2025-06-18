namespace CsvProcessor.DAL.Interface;

public interface IShippingRepository

{
    public Task<List<string>> BulkInsertShippingClassAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict);

}
