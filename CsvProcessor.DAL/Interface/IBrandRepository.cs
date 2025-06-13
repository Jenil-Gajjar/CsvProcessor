namespace CsvProcessor.DAL.Interface;

public interface IBrandRepository
{

    public Task BulkInsertBrandAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict);


}
