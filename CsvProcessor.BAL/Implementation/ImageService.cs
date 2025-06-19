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
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public async Task<ImageServiceDto> ProcessImagesAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {

        ImageServiceDto imageServiceDto = new();
        var tasks = new List<(string sku, string url, bool is_primary)>(records.Count() * 2);
        var processedUrls = new ConcurrentDictionary<string, bool>();

        foreach (var record in records)
        {
            if (!record.TryGetValue("product_sku", out var sku)) continue;
            if (string.IsNullOrEmpty(sku.ToString()) || !SkuIdDict.ContainsKey(sku.ToString()!)) continue;
            int index = 0;
            foreach (var kv in record)
            {
                if (kv.Key.StartsWith("image_url") && !string.IsNullOrWhiteSpace(kv.Value?.ToString()))
                {
                    tasks.Add((sku.ToString(), kv.Value.ToString(), index == 0)!);
                    index++;
                }
            }
        }

        var taskList = tasks.Select(async (item) =>
        {
            string hash = GenerateHash(item.url);
            string imageFileName = $"{hash[..16]}.jpg";
            string fullPath = Path.Combine(_imageDir, imageFileName);

            if (!SkuIdDict.TryGetValue(item.sku, out var id)) return;

            if (_cache.TryGetValue(item.url, out bool isCorrectUrl))
            {
                if (isCorrectUrl)
                {
                    processedUrls.TryAdd(item.url, true);
                    imageServiceDto.ImageList.Add(new ProductImageDto
                    {
                        product_id = id,
                        image_path = imageFileName,
                        is_primary = item.is_primary
                    });
                }
                else
                {
                    AddErrorMessage(imageServiceDto.ErrorMessageList, item.sku, item.url);
                }
                return;
            }


            if (!File.Exists(fullPath))
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var response = await _httpClient.GetAsync(item.url, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        processedUrls.TryAdd(item.url, true);
                        _cache.Set(item.url, true, TimeSpan.FromHours(1));
                        imageServiceDto.ImageList.Add(new ProductImageDto
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
                        _cache.Set(item.url, false, TimeSpan.FromHours(1));
                        AddErrorMessage(imageServiceDto.ErrorMessageList, item.sku, item.url);
                    }
                }
                catch
                {
                    _cache.Set(item.url, false, TimeSpan.FromHours(1));
                    AddErrorMessage(imageServiceDto.ErrorMessageList, item.sku, item.url);
                }
            }
            else
            {
                processedUrls.TryAdd(item.url, true);
                _cache.Set(item.url, true, TimeSpan.FromHours(1));
                imageServiceDto.ImageList.Add(new ProductImageDto
                {
                    product_id = id,
                    image_path = imageFileName,
                    is_primary = item.is_primary
                });
            }
        });

        await Task.WhenAll(taskList);
        imageServiceDto.ProcessedUrls = processedUrls.Count;
        return imageServiceDto;
    }
    public static void AddErrorMessage(HashSet<string> ErrorMessageList, string sku, string url)
    {
        string urlMessage = $"{sku}:Failed Downloading Image from Url {url}";
        lock (ErrorMessageList)
        {
            ErrorMessageList.Add(urlMessage);
        }
    }
}
