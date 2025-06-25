using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvProcessor.BAL.Interface;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.Constants;
using CsvProcessor.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
namespace CsvProcessor.BAL.Implementation;

public class CsvProcessorService : ICsvProcessorService
{
    private readonly ILogger<CsvProcessorService> _logger;
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
        IImageService imageService,
        ILogger<CsvProcessorService> logger
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
        _logger = logger;
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
            int batchSize = 5000;
            List<Dictionary<string, object>> batch = new(batchSize);
            ConcurrentDictionary<string, bool> processedGlobalUrls = new();

            while (csv.Read())
            {
                try
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
                    if (ValidateDictionary(dict, RowCount, summary))
                        batch.Add(dict);

                    if (batch.Count >= batchSize)
                    {
                        await ProcessBatchAsync(batch, summary, processedGlobalUrls);
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
                await ProcessBatchAsync(batch, summary, processedGlobalUrls);
                batch.Clear();
            }
            summary.RowCount = RowCount;
            summary.TotalSuccessfullUrls = processedGlobalUrls.Count;

        }
        catch (Exception e)
        {
            summary.Errors.Add(e.Message);
        }
        _logger.LogInformation("File Processed Successfully!");

        return summary;
    }
    private async Task ProcessBatchAsync(IEnumerable<Dictionary<string, object>> batch, ImportSummaryDto summary, ConcurrentDictionary<string, bool> processedGlobalUrls
)
    {

        ProductDto productDto = await _productRepository.BulkUpsertProductAsync(batch, summary);
        if (productDto.SkuToIdDict == null) throw new Exception("Sku To Id Dictionary Is Null");

        summary.InsertedRecords += productDto.InsertedRecords;
        summary.UpdatedRecords += productDto.UpdatedRecords;

        try
        {
            await _categoryRepository.BulkInsertCategoryAsync(batch, productDto.SkuToIdDict, summary);
        }
        catch (Exception e)
        {
            summary.Errors.Add($"Category: {e.Message}");
        }
        try
        {
            await _brandRepository.BulkInsertBrandAsync(batch, productDto.SkuToIdDict, summary);
        }
        catch (Exception e)
        {
            summary.Errors.Add($"Brand: {e.Message}");
        }
        try
        {
            await _shippingRepository.BulkInsertShippingClassAsync(batch, productDto.SkuToIdDict);
        }
        catch (Exception e)
        {
            summary.Errors.Add($"Shipping Class: {e.Message}");
        }
        try
        {
            await _variantRepository.BulkInsertVariantAsync(batch, productDto.SkuToIdDict);
        }
        catch (Exception e)
        {
            summary.Errors.Add($"Variant: {e.Message}");
        }
        try
        {
            var UpdatedInventoryCount = await _inventoryRepository.BulkInsertInventoryAsync(batch, productDto.SkuToIdDict);
            summary.UpdatedInventoryCount += UpdatedInventoryCount;
        }
        catch (Exception e)
        {
            summary.Errors.Add($"Inventory: {e.Message}");
        }
        try
        {
            ImageServiceDto imageServiceDto = await _imageService.ProcessImagesAsync(batch, productDto.SkuToIdDict, processedGlobalUrls);
            summary.Errors.AddRange(imageServiceDto.ErrorMessageList);
            await _productImageRepository.BulkInsertImagesAsync(imageServiceDto.ImageList);
        }
        catch (Exception e)
        {
            summary.Errors.Add($"Error In Image Service :{e.Message}");
        }


    }
    private static bool IsValidDimention(string? value)
    {
        if (string.IsNullOrWhiteSpace(value?.Trim())) return true;
        string pattern = @"^\d+(\.\d+)?x\d+(\.\d+)?x\d+(\.\d+)?$";
        return Regex.IsMatch(value!, pattern);
    }
    private static bool ValidateDictionary(IDictionary<string, object> dict, int RowCount, ImportSummaryDto summary)
    {
        bool IsValid = true;
        HashSet<string> validStatues = new() {
            Constants.active_status,
            Constants.inactive_status,
            Constants.discontinued_status
        };

        string sku = GetString(dict, Constants.product_sku);
        string name = GetString(dict, Constants.product_name);
        string status = GetString(dict, Constants.status);
        string base_price = GetString(dict, Constants.base_price);
        string weight_kg = GetString(dict, Constants.weight_kg);
        string dimensions_cm = GetString(dict, Constants.dimensions_cm);
        string shipping_class = GetString(dict, Constants.shipping_class);

        if (string.IsNullOrWhiteSpace(sku))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} : Invalid sku ");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {sku}: Invalid name ");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            dict[Constants.status] = status = Constants.active_status;
            summary.Warnings.Add($"Row {RowCount} {sku}:Invalid Status defaulted to 'active'");
        }

        if (!validStatues.Contains(status))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {sku}: Invalid Status ");
        }

        if (string.IsNullOrWhiteSpace(shipping_class))
        {
            dict[Constants.shipping_class] = Constants.standard_shipping_class;
            summary.Warnings.Add($"Row {RowCount} {sku}:Invalid shipping class defaulted to 'standard'");
        }

        if (!IsValidDimention(dimensions_cm))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {sku}:Invalid dimensions. Use the format LxWxH");
        };

        if (!HasAtLeastOneImageUrl(dict))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {sku}:At least one image is required");
        }

        if (!IsPositive(base_price))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {sku}:Base Price is not postitive");
        }

        if (!string.IsNullOrWhiteSpace(weight_kg))
        {
            if (!IsPositive(weight_kg))
            {
                IsValid = false;
                summary.Warnings.Add($"Row {RowCount} {sku}:Weight is not postitive");
            }
        }

        return IsValid;
    }

    public static bool HasAtLeastOneImageUrl(IDictionary<string, object> dict)
    {
        return dict.Keys.Any(k => k.StartsWith(Constants.image_url) && !string.IsNullOrWhiteSpace(dict[k]?.ToString()));
    }


    public static string GetString(IDictionary<string, object> dict, string key)
    {
        return dict.ContainsKey(key) ? dict[key].ToString()?.Trim().ToLower() ?? string.Empty : string.Empty;
    }

    public static bool IsPositive(string value)
    {
        return decimal.TryParse(value, out var result) && result > 0;
    }
}