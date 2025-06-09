namespace CsvProcessor.DAL.Interface;

public interface ICategoryRepository
{
    public Task InsertCategoryAsync(string path, int productid);

}
