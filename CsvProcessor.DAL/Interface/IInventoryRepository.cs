namespace CsvProcessor.DAL.Interface;

public interface IInventoryRepository
{
    public Task SyncInventoryAsync(IDictionary<string, object> dict, int productid);

}
