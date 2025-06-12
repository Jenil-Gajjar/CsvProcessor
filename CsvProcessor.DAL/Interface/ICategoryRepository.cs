namespace CsvProcessor.DAL.Interface;

public interface ICategoryRepository
{
    public Task InsertCategoryAsync(string path, int productid);
    public Task BulkInsertCategoryAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict);



}
