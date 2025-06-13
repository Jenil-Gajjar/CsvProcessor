using CsvProcessor.BAL.Interface;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using System.Diagnostics;
using CsvProcessor.Models.DTOs;
using CsvProcessor.BAL.Helper;
using System.Collections.Concurrent;

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

    public async Task<(ConcurrentBag<ProductImageDto>, ConcurrentDictionary<string, ConcurrentBag<string>>)> ProcessImagesAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);
        var tasks = new List<(string sku, string url, bool is_primary)>();
        var seenUrls = new ConcurrentDictionary<string, bool>();
        var imageList = new ConcurrentBag<ProductImageDto>();
        var ImageMessageList = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        records.ToList().ForEach(record =>
        {
            if (!record.TryGetValue("product_sku", out var sku)) return;
            if (string.IsNullOrEmpty(sku.ToString()) || !SkuIdDict.ContainsKey(sku.ToString()!)) return;

            var urls = record.Where(kv => kv.Key.StartsWith("image_url") && !string.IsNullOrWhiteSpace(kv.Value?.ToString())).Select((kv, ix) => (sku.ToString()!, kv.Value.ToString()!, ix == 0));

            if (urls != null && urls.Any())
                tasks.AddRange(urls);
        });

        var taskList = tasks.Select(item => Task.Run(async () =>
        {
            string hash = GenerateHash(item.url);
            string imageFileName = $"{hash[..16]}.jpg";
            string fullPath = Path.Combine(_imageDir, imageFileName);

            if (!seenUrls.TryAdd(item.url, true))
            {
                imageList.Add(new ProductImageDto
                {
                    product_id = SkuIdDict.TryGetValue(item.sku, out var id) ? id : 0,
                    image_path = imageFileName,
                    is_primary = item.is_primary
                });
            }
            else
            {
                if (!File.Exists(fullPath))
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(item.url);
                        if (response.IsSuccessStatusCode)
                        {
                            byte[] ImageBytes = await response.Content.ReadAsByteArrayAsync();
                            imageList.Add(new ProductImageDto
                            {

                                product_id = SkuIdDict.TryGetValue(item.sku, out var id) ? id : 0,
                                image_path = imageFileName,
                                is_primary = item.is_primary
                            });
                            ImageProcessingQueue.ImageQueue.Enqueue(new ImageProcessDto()
                            {
                                ImagePath = fullPath,
                                ImageBytes = ImageBytes
                            });
                        }
                        else
                        {
                            ImageMessageList.AddOrUpdate(
                            item.sku,
                            // Add: Create a new list if the key doesn't exist
                            _ => new ConcurrentBag<string>() { $"{item.sku}:Failed Downloading Image from Url {item.url}" },
                            // Update: Add the new value to the existing list
                            (_, list) =>
                                {

                                    list.Add($"{item.sku}:Failed Downloading Image from Url {item.url}");
                                    return list;
                                }
                            );

                        }
                    }
                    catch (Exception e)
                    {
                        ImageMessageList.AddOrUpdate(
                            item.sku,
                            _ => new ConcurrentBag<string>() { $"{item.sku}:Failed Downloading Image from Url {item.url}" },
                            (_, list) =>
                            {
                                lock (list)
                                {
                                    list.Add($"{item.sku}:Failed Downloading Image from Url {item.url}");
                                }
                                return list;
                            }
                        );
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    imageList.Add(new ProductImageDto
                    {
                        product_id = SkuIdDict.TryGetValue(item.sku, out var id) ? id : 0,
                        image_path = imageFileName,
                        is_primary = item.is_primary
                    });
                }
            }
        }));


        await Task.WhenAll(taskList);
        return (imageList, ImageMessageList);
    }

}
