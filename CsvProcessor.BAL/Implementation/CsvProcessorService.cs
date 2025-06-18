using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvProcessor.BAL.Interface;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
namespace CsvProcessor.BAL.Implementation;

public class CsvProcessorService : ICsvProcessorService
{
    private readonly IProductRepository _productRepository;

    private readonly ILogger<CsvProcessorService> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IShippingRepository _shippingRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IImageService _imageService;
    private readonly IProductImageRepository _productImageRepository;

    public CsvProcessorService(
        ILogger<CsvProcessorService> logger,
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
        _logger = logger;
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
                        (
                            Dictionary<string, int> recordCounts,
                            List<string> ErrorMessageList,
                            List<string> WarningMessageList,
                            int TotalSuccessfullUrls
                        ) = await ProcessBatchAsync(batch);

                        summary.InsertedRecords += recordCounts["InsertedRecords"];
                        summary.UpdatedRecords += recordCounts["UpdatedRecords"];
                        summary.TotalSuccessfullUrls += TotalSuccessfullUrls;
                        summary.Errors.AddRange(ErrorMessageList);
                        summary.Warnings.AddRange(WarningMessageList);
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
                (
                    Dictionary<string, int> recordCounts,
                    List<string> ErrorMessageList,
                    List<string> WarningMessageList,
                    int TotalSuccessfullUrls
                ) = await ProcessBatchAsync(batch);
                summary.InsertedRecords += recordCounts["InsertedRecords"];
                summary.UpdatedRecords += recordCounts["UpdatedRecords"];
                summary.TotalSuccessfullUrls += TotalSuccessfullUrls;
                summary.Errors.AddRange(ErrorMessageList);
                summary.Warnings.AddRange(WarningMessageList);
                batch.Clear();
            }

            summary.RowCount = RowCount;

        }
        catch (Exception e)
        {
            _logger.LogError("{message}", e.Message);
            summary.Errors.Add(e.Message);
        }

        return summary;
    }

    private async Task<(Dictionary<string, int>, List<string>, List<string>, int)>
    ProcessBatchAsync(IEnumerable<Dictionary<string, object>> batch)
    {

        List<string> ErrorMessageList = new();
        List<string> WarningMessageList = new();
        int TotalSuccessfullUrls = 0;
        (Dictionary<string, int> SkuIdDict, Dictionary<string, int> recordCounts) = await _productRepository.BulkUpsertProductAsync(batch);

        try
        {
            await _categoryRepository.BulkInsertCategoryAsync(batch, SkuIdDict).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError("Category:{Message}", e.Message);
        }
        try
        {

            await _brandRepository.BulkInsertBrandAsync(batch, SkuIdDict).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError("Brand:{Message}", e.Message);

        }
        try
        {
            var warnings = await _shippingRepository.BulkInsertShippingClassAsync(batch, SkuIdDict).ConfigureAwait(false);
            WarningMessageList.AddRange(warnings);
        }
        catch (Exception e)
        {
            _logger.LogError("Shipping Class:{Message}", e.Message);
        }
        try
        {
            await _variantRepository.BulkInsertVariantAsync(batch, SkuIdDict).ConfigureAwait(false);

        }
        catch (Exception e)
        {
            _logger.LogError("Variant:{Message}", e.Message);
        }
        try
        {

            await _inventoryRepository.BulkInsertInventoryAsync(batch, SkuIdDict).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError("Inventory:{Message}", e.Message);
        }
        try
        {
            (ConcurrentBag<ProductImageDto> set, ConcurrentDictionary<string, HashSet<string>> ImageMessageList, int ImageDownloads) = await _imageService.ProcessImagesAsync(batch, SkuIdDict);
            ImageMessageList.Values.ToList().ForEach(ErrorMessageList.AddRange);
            TotalSuccessfullUrls += ImageDownloads;

            await _productImageRepository.BulkInsertImagesAsync(set);

        }
        catch (Exception e)
        {
            _logger.LogError("Image Service:{Message}", e.Message);
        }

        return (recordCounts, ErrorMessageList, WarningMessageList, TotalSuccessfullUrls);

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
            throw new Exception($"Row {RowCount} {dict["product_sku"]}: Invalid price format");
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
            throw new Exception($"Row {RowCount} {dict["product_sku"]}: Invalid Weight format");
        }
    }

}