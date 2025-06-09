namespace CsvProcessor.DAL.Interface;

public interface IVariantRepository
{
    public Task SyncVariantAsync(IDictionary<string,object> dict, int productid);

}
