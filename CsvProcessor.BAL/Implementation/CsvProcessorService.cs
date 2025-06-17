using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvProcessor.BAL.Interface;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.DTOs;
using Microsoft.AspNetCore.Http;
namespace CsvProcessor.BAL.Implementation;

public class CsvProcessorService : ICsvProcessorService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IShippingRepository _shippingRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IImageService _imageService;
    private readonly IProductImageRepository _productImageRepository;

    public CsvProcessorService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IBrandRepository brandRepository,
        IVariantRepository variantRepository,
        IShippingRepository shippingRepository,
        IInventoryRepository inventoryRepository,
        IProductImageRepository productImageRepository,
        IImageService imageService
    )
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _variantRepository = variantRepository;
        _shippingRepository = shippingRepository;
        _inventoryRepository = inventoryRepository;
        _productImageRepository = productImageRepository;
        _imageService = imageService;
    }


    public async Task<ImportSummaryDto> ProcessCsvAsync(IFormFile file)
    {
        ImportSummaryDto summary = new();
        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                HasHeaderRecord = true
            });
            csv.Read();
            csv.ReadHeader();
            int RowCount = 0;
            int InsertedRecords = 0;
            int UpdatedRecords = 0;
            int batchSize = 1000;
            List<Dictionary<string, object>> batch = new(batchSize);
            while (csv.Read())
            {
                RowCount++;
                var dict = new Dictionary<string, object>();
                if (csv.HeaderRecord != null)
                {
                    foreach (var header in csv.HeaderRecord)
                    {
                        dict[header] = csv.GetField(header)!;
                    }
                }
                try
                {
                    ValidateDictionary(dict, RowCount);
                    batch.Add(dict);
                    if (batch.Count >= batchSize)
                    {
                        (Dictionary<string, int> recordCounts, List<string> MessageList) = await ProcessBatchAsync(batch);
                        InsertedRecords += recordCounts["InsertedRecords"];
                        UpdatedRecords += recordCounts["UpdatedRecords"];
                        summary.Errors.AddRange(MessageList);
                        batch.Clear();
                    }
                }
                catch (Exception e)
                {
                    summary.Errors.Add(e.Message);
                }
            }
            if (batch.Any())
            {
                (Dictionary<string, int> recordCounts, List<string> MessageList) = await ProcessBatchAsync(batch);
                InsertedRecords += recordCounts["InsertedRecords"];
                UpdatedRecords += recordCounts["UpdatedRecords"];
                summary.Errors.AddRange(MessageList);
                batch.Clear();
            }

            summary.RowCount = RowCount;
            summary.InsertedRecords = InsertedRecords;
            summary.UpdatedRecords = UpdatedRecords;

        }
        catch (Exception e)
        {
            summary.Errors.Add(e.Message);
        }

        return summary;
    }

    private async Task<(Dictionary<string, int> recordCounts, List<string>)> ProcessBatchAsync(IEnumerable<Dictionary<string, object>> batch)
    {

        List<string> MessageList = new();

        (Dictionary<string, int> SkuIdDict, Dictionary<string, int> recordCounts) = await _productRepository.BulkUpsertProductAsync(batch);

        try
        {
            await _categoryRepository.BulkInsertCategoryAsync(batch, SkuIdDict).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Category:{e.Message}");
        }
        try
        {

            await _brandRepository.BulkInsertBrandAsync(batch, SkuIdDict).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Brand:{e.Message}");
        }
        try
        {
            await _shippingRepository.BulkInsertShippingClassAsync(batch, SkuIdDict).ConfigureAwait(false);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Shipping Class:{e.Message}");
        }
        try
        {
            await _variantRepository.BulkInsertVariantAsync(batch, SkuIdDict).ConfigureAwait(false);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Variant:{e.Message}");
        }
        try
        {

            await _inventoryRepository.BulkInsertInventoryAsync(batch, SkuIdDict).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Inventory:{e.Message}");
        }
        try
        {
            (ConcurrentBag<ProductImageDto> set, ConcurrentDictionary<string, HashSet<string>> ImageMessageList) = await _imageService.ProcessImagesAsync(batch, SkuIdDict);

            ImageMessageList.Values.ToList().ForEach(MessageList.AddRange);
            await _productImageRepository.BulkInsertImagesAsync(set);

        }
        catch (Exception e)
        {
            Console.WriteLine($"Image Service:{e.Message}");
        }

        return (recordCounts, MessageList);

    }

    private static bool IsValidDimention(string? value)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) return false;
        string pattern = @"^\d+(\.\d+)?x\d+(\.\d+)?x\d+(\.\d+)?$";
        return Regex.IsMatch(value!, pattern);
    }
    private static void ValidateDictionary(IDictionary<string, object> dict, int RowCount)
    {
        if (!IsValidDimention(dict["dimensions_cm"].ToString())) throw new Exception($"Row {RowCount} {dict["product_sku"]}:Invalid dimensions. Use the format LxWxH");

        var hasImage = dict.Keys.Any(k => k.StartsWith("image_url") && !string.IsNullOrWhiteSpace(dict[k]?.ToString()));
        if (!hasImage) throw new Exception($"Row {RowCount} {dict["product_sku"]}:At least one image is required");

        if (decimal.TryParse(dict["base_price"].ToString(), out var basePrice))
        {
            if (basePrice <= 0)
            {
                throw new Exception($"Row {RowCount} {dict["product_sku"]}:Base Price is not postitive");
            }
        }
        else
        {
            throw new Exception($"Row {RowCount} {dict["product_sku"]}:Base Price is not In Correct Format");
        }
        if (decimal.TryParse(dict["weight_kg"].ToString(), out var weightKg))
        {
            if (weightKg <= 0)
            {
                throw new Exception($"Row {RowCount} {dict["product_sku"]}:Weight is not postitive");
            }
        }
        else
        {
            throw new Exception($"Row {RowCount} {dict["product_sku"]}:Weight is not In Correct Format");
        }
    }

}