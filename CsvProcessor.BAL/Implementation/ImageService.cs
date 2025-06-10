using CsvProcessor.BAL.Interface;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using Dapper;

namespace CsvProcessor.BAL.Implementation;

public class ImageService : IImageService
{
    private readonly string _conn;

    private readonly string _imageDir = Path.Combine("wwwroot", "images");
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
        bool is_primary = true;
        foreach (var kv in dict)
        {
            if (kv.Key.StartsWith("image_url") && !string.IsNullOrWhiteSpace(kv.Value?.ToString()))
            {
                string url = kv.Value?.ToString()!;
                string hash = GenerateHash(url);
                string imageFileName = $"{hash[..16]}.jpg";
                string fullPath = Path.Combine(_imageDir, imageFileName);
                if (!File.Exists(fullPath))
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                            await File.WriteAllBytesAsync(fullPath, imageBytes);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Image Not Found!");
                        continue;
                    }
                }
                await conn.ExecuteAsync("select fn_product_image_insert(@p_product_id, @p_image_path, @p_is_primary)", new
                {
                    p_product_id = productid,
                    p_image_path = imageFileName,
                    p_is_primary = is_primary,
                });
            }
        }
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
