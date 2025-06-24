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
            await _shippingRepository.BulkInsertShippingClassAsync(batch, productDto.SkuToIdDict, summary);
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
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) return false;
        string pattern = @"^\d+(\.\d+)?x\d+(\.\d+)?x\d+(\.\d+)?$";
        return Regex.IsMatch(value!, pattern);
    }
    private static bool ValidateDictionary(IDictionary<string, object> dict, int RowCount, ImportSummaryDto summary)
    {
        bool IsValid = true;
        if (!IsValidDimention(dict["dimensions_cm"].ToString()))
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {dict["product_sku"]}:Invalid dimensions. Use the format LxWxH");
        };

        var hasValidImage = dict.Keys.Any(k => k.StartsWith("image_url") && !string.IsNullOrWhiteSpace(dict[k]?.ToString()));
        if (!hasValidImage)
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {dict["product_sku"]}:At least one image is required");
        }

        if (decimal.TryParse(dict["base_price"].ToString(), out var basePrice))
        {
            if (basePrice <= 0)
            {
                IsValid = false;
                summary.Warnings.Add($"Row {RowCount} {dict["product_sku"]}:Base Price is not postitive");
            }
        }
        else
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {dict["product_sku"]}: Invalid price format");
        }
        if (decimal.TryParse(dict["weight_kg"].ToString(), out var weightKg))
        {
            if (weightKg <= 0)
            {
                IsValid = false;
                summary.Warnings.Add($"Row {RowCount} {dict["product_sku"]}:Weight is not postitive");
            }
        }
        else
        {
            IsValid = false;
            summary.Warnings.Add($"Row {RowCount} {dict["product_sku"]}: Invalid Weight format");
        }
        return IsValid;
    }

}