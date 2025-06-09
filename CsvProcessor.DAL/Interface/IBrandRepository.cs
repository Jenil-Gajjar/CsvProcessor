namespace CsvProcessor.DAL.Interface;

public interface IBrandRepository
{
    public Task InsertBrandAsync(string brandName, int productid);

}
