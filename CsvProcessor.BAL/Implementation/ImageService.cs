using CsvProcessor.BAL.Interface;
using System.Security.Cryptography;
using System.Text;
using CsvProcessor.Models.DTOs;
using CsvProcessor.BAL.Helper;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace CsvProcessor.BAL.Implementation;

public class ImageService : IImageService
{
    private readonly static string _imageDir = Path.Combine("wwwroot", "images");
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    public ImageService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    private static string GenerateHash(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(bytes);
        StringBuilder hashString = new();
        hashBytes.ToList().ForEach(b => hashString.Append(b.ToString("x2")));
        return hashString.ToString();
    }

    public async Task<(ConcurrentBag<ProductImageDto>, ConcurrentDictionary<string, HashSet<string>>)> ProcessImagesAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict)
    {
        var tasks = new List<(string sku, string url, bool is_primary)>();
        var imageList = new ConcurrentBag<ProductImageDto>();
        var ImageMessageList = new ConcurrentDictionary<string, HashSet<string>>();

        records.ToList().ForEach(record =>
        {
            if (!record.TryGetValue("product_sku", out var sku)) return;
            if (string.IsNullOrEmpty(sku.ToString()) || !SkuIdDict.ContainsKey(sku.ToString()!)) return;

            var urls = record.Where(kv => kv.Key.StartsWith("image_url") && !string.IsNullOrWhiteSpace(kv.Value?.ToString())).Select((kv, ix) => (sku.ToString()!, kv.Value.ToString()!, ix == 0));

            if (urls != null && urls.Any())
                tasks.AddRange(urls);
        });

        var taskList = tasks.Select(async (item) =>
        {
            string hash = GenerateHash(item.url);
            string imageFileName = $"{hash[..16]}.jpg";
            string fullPath = Path.Combine(_imageDir, imageFileName);

            if (!SkuIdDict.TryGetValue(item.sku, out var id)) return;

            if (_cache.TryGetValue(item.url, out var res))
            {
                bool isCorrectUrl = Convert.ToBoolean(res);
                if (isCorrectUrl)
                {
                    imageList.Add(new ProductImageDto
                    {

                        product_id = id,
                        image_path = imageFileName,
                        is_primary = item.is_primary
                    });
                }
                else
                {
                    string urlMessage = $"{item.sku}:Failed Downloading Image from Url {item.url}";
                    ImageMessageList.AddOrUpdate(
                                            item.sku,
                                            // Add: Create a new list if the key doesn't exist
                                            _ => new HashSet<string>() { urlMessage },
                                            // Update: Add the new value to the existing list
                                            (_, list) =>
                                                {
                                                    lock (list)
                                                    {
                                                        list.Add(urlMessage);
                                                        return list;
                                                    }
                                                }
                                        );
                }
            }
            else
            {

                if (!File.Exists(fullPath))
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var req = new HttpRequestMessage(HttpMethod.Head, item.url);

                        var response = await _httpClient.GetAsync(item.url, cts.Token);
                        if (response.IsSuccessStatusCode)
                        {
                            _cache.Set(item.url, true, TimeSpan.FromHours(1));
                            imageList.Add(new ProductImageDto
                            {
                                product_id = id,
                                image_path = imageFileName,
                                is_primary = item.is_primary
                            });
                            ImageProcessingQueue.ImageQueue.Enqueue(new ImageProcessDto()
                            {
                                ImagePath = fullPath,
                                ResponseContent = response.Content
                            });
                        }
                        else
                        {
                            throw new Exception("Image Not Found!");
                        }
                    }
                    catch
                    {
                        string urlMessage = $"{item.sku}:Failed Downloading Image from Url {item.url}";
                        _cache.Set(item.url, false, TimeSpan.FromHours(1));
                        ImageMessageList.AddOrUpdate(
                                        item.sku,
                                        // Add: Create a new list if the key doesn't exist
                                        _ => new HashSet<string>() { urlMessage },
                                        // Update: Add the new value to the existing list
                                        (_, list) =>
                                            {
                                                lock (list)
                                                {
                                                    list.Add(urlMessage);
                                                    return list;
                                                }
                                            }
                                    );
                    }
                }
                else
                {
                    _cache.Set(item.url, true, TimeSpan.FromHours(1));
                    imageList.Add(new ProductImageDto
                    {
                        product_id = id,
                        image_path = imageFileName,
                        is_primary = item.is_primary
                    });
                }
            }
        });


        await Task.WhenAll(taskList);
        return (imageList, ImageMessageList);
    }

}
