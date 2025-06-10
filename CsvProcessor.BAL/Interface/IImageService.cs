namespace CsvProcessor.BAL.Interface;

public interface IImageService
{
    public Task InsertImagesAsync(
        IDictionary<string, object> dict,
        int productid
    );
}
