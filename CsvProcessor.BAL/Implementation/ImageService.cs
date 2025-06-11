using CsvProcessor.BAL.Interface;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using System.Diagnostics;

namespace CsvProcessor.BAL.Implementation;

public class ImageService : IImageService
{
    private readonly string _conn;
    private readonly static string _imageDir = Path.Combine("wwwroot", "images");
    private readonly HttpClient _httpClient;
    public ImageService(IConfiguration configuration, HttpClient httpClient)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
        _httpClient = httpClient;
    }
    public async Task InsertImagesAsync(
        IDictionary<string, object> dict,
        int productid
    )
    {
        using var conn = new NpgsqlConnection(_conn);
        var urls = dict.Where(kv => kv.Key.StartsWith("image_url") && !string.IsNullOrWhiteSpace(kv.Value?.ToString())).Select((kv, ix) => (url: kv.Value?.ToString(), is_primary: ix == 0));

        Stopwatch imageStopWatch = Stopwatch.StartNew();
        await Parallel.ForEachAsync(urls, async (url, _) =>
        {   
            string hash = GenerateHash(url.url!);
            string imageFileName = $"{hash[..16]}.jpg";
            string fullPath = Path.Combine(_imageDir, imageFileName);
            bool is_primary = url.is_primary;

            if (!File.Exists(fullPath))
            {
                try
                {
                    var response = await _httpClient.GetAsync(url.url);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync(_);
                        await File.WriteAllBytesAsync(fullPath, imageBytes, _);
                        // ThumbnailQueue.thumbnailQueue.Enqueue(fullPath);
                    }
                    else
                    {
                        Console.WriteLine("Image Not Found!");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
                await conn.ExecuteAsync("select fn_product_image_insert(@p_product_id, @p_image_path, @p_is_primary)", new
                {
                    p_product_id = productid,
                    p_image_path = imageFileName,
                    p_is_primary = is_primary,
                });
            }
        });

        imageStopWatch.Stop();
    }

    private static string GenerateHash(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(bytes);
        StringBuilder hashString = new();

        foreach (var b in hashBytes)
        {
            hashString.Append(b.ToString("x2"));
        }
        return hashString.ToString();
    }

}
