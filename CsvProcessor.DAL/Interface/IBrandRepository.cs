namespace CsvProcessor.DAL.Interface;

public interface IBrandRepository
{
    public Task InsertBrandAsync(string brandName, int productid);

    public Task BulkInsertBrandAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict);


}
