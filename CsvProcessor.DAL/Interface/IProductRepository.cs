namespace CsvProcessor.DAL.Interface;

public interface IProductRepository
{
    public Task<int> UpsertProductAsync(IDictionary<string, object> dict);
}
