namespace CsvProcessor.DAL.Interface;

public interface IShippingRepository

{
    public Task InsertShippingClassAsync(string className, int productid);

}
