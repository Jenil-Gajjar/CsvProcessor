using CsvProcessor.BAL.Interface;
using System.Security.Cryptography;
using System.Text;
using CsvProcessor.Models.DTOs;
using CsvProcessor.BAL.Helper;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

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
        if (!Directory.Exists(_imageDir))
            Directory.CreateDirectory(_imageDir);
    }

    public async Task<ImageServiceDto> ProcessImagesAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict, ConcurrentDictionary<string, bool> processedGlobalUrls)
    {

        ImageServiceDto imageServiceDto = new();
        var ItemList = new List<(string sku, string url, bool is_primary)>(records.Count() * 2);

        foreach (var record in records)
        {
            if (!record.TryGetValue("product_sku", out var sku)) continue;
            if (string.IsNullOrEmpty(sku.ToString()) || !SkuIdDict.ContainsKey(sku.ToString()?.ToLower()!)) continue;
            int index = 0;
            foreach (var kv in record)
            {
                if (kv.Key.StartsWith("image_url") && !string.IsNullOrWhiteSpace(kv.Value?.ToString()))
                {
                    ItemList.Add((sku.ToString()?.ToLower(), kv.Value.ToString(), index == 0)!);
                    index++;
                }
            }
        }

        List<Task>? taskList = new();

        foreach (var item in ItemList)
        {
            taskList.Add(ProcessUrlAsync(item, SkuIdDict, imageServiceDto, processedGlobalUrls));
        }

        await Task.WhenAll(taskList);
        return imageServiceDto;
    }

    private async Task ProcessUrlAsync(
        (string sku, string url, bool is_primary) item,
        IDictionary<string, int> SkuIdDict,
        ImageServiceDto imageServiceDto,
        ConcurrentDictionary<string, bool> processedGlobalUrls
    )
    {
        var (sku, url, is_primary) = item;
        string hash = GenerateHash(url);
        string imageFileName = $"{hash[..16]}.jpg";
        string fullPath = Path.Combine(_imageDir, imageFileName);
        if (!SkuIdDict.TryGetValue(sku, out var id)) return;

        if (!IsValidUrl(url))
        {
            LogError(imageServiceDto, $"{sku}:Invalid url:{url}");
            return;
        }
        if (_cache.TryGetValue(url, out bool isCorrectUrl))
        {
            if (isCorrectUrl)
            {
                AddImage(imageServiceDto, id, imageFileName, is_primary);
                if (!File.Exists(fullPath))
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var response = await _httpClient.GetAsync(url, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        _cache.Set(url, true, TimeSpan.FromHours(1));
                        processedGlobalUrls.TryAdd(url, true);

                        ImageProcessingQueue.ImageQueue.Enqueue(new ImageProcessDto()
                        {
                            ImagePath = fullPath,
                            ResponseContent = response.Content
                        });
                    }
                    return;
                }
                processedGlobalUrls.TryAdd(url, true);

            }
            else
            {
                LogError(imageServiceDto, $"{sku}:Image URL {url} failed to download");
            }
            return;
        }

        if (File.Exists(fullPath))
        {
            processedGlobalUrls.TryAdd(url, true);
            _cache.Set(url, true, TimeSpan.FromHours(1));
            AddImage(imageServiceDto, id, imageFileName, is_primary);
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await _httpClient.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                processedGlobalUrls.TryAdd(url, true);
                _cache.Set(url, true, TimeSpan.FromHours(1));
                AddImage(imageServiceDto, id, imageFileName, is_primary);
                ImageProcessingQueue.ImageQueue.Enqueue(new ImageProcessDto()
                {
                    ImagePath = fullPath,
                    ResponseContent = response.Content
                });
                return;
            }
            TimeSpan timeSpan = TimeSpan.FromSeconds(10);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                timeSpan = TimeSpan.FromHours(1);
            }

            _cache.Set(url, false, timeSpan);
            LogError(imageServiceDto, $"{sku}:Image URL {url} failed to download");
        }
        catch
        {
            _cache.Set(url, false, TimeSpan.FromHours(1));
            LogError(imageServiceDto, $"{sku}:Image URL {url} failed to download");
        }
    }

    private static string GenerateHash(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
    private static void LogError(ImageServiceDto imageServiceDto, string message)
    {
        lock (imageServiceDto.ErrorMessageList)
        {
            imageServiceDto.ErrorMessageList.Add(message);
        }
    }
    private static void AddImage(ImageServiceDto imageServiceDto, int id, string imageFileName, bool is_primary)
    {
        imageServiceDto.ImageList.Add(new ProductImageDto
        {
            Productid = id,
            ImagePath = imageFileName.Trim().ToLower(),
            IsPrimary = is_primary
        });
    }
    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
