using System.Globalization;
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


    public CsvProcessorService(IProductRepository productRepository, ICategoryRepository categoryRepository, IBrandRepository brandRepository, IVariantRepository variantRepository, IShippingRepository shippingRepository, IInventoryRepository inventoryRepository, IImageService imageService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _variantRepository = variantRepository;
        _shippingRepository = shippingRepository;
        _inventoryRepository = inventoryRepository;
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
            List<Dictionary<string, object>> records = new();
            while (csv.Read())
            {
                var dict = new Dictionary<string, object>();
                if (csv.HeaderRecord != null)
                {
                    foreach (var header in csv.HeaderRecord)
                    {
                        dict[header] = csv.GetField(header)!;
                    }
                }
                records.Add(dict);
            }
            int i = 1;
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount - 1
            };
            await Parallel.ForEachAsync(records, options, async (dict, cancellationToken) =>
            {
                try
                {
                    var dimensions = dict["dimensions_cm"].ToString();
                    if (!IsValidDimention(dimensions)) throw new Exception($"{i++}:Invalid dimensions. Use the format LxWxH");

                    var hasImage = dict.Keys.Any(k => k.StartsWith("image_url") && !string.IsNullOrWhiteSpace(dict[k]?.ToString()));
                    if (!hasImage) throw new Exception($"{i++}:At least one image is required");

                    if (decimal.TryParse(dict["base_price"].ToString(), out var basePrice))
                    {
                        if (basePrice <= 0)
                        {
                            throw new Exception($"{i++}:Base Price is not postitive");
                        }
                    }
                    else
                    {
                        throw new Exception($"{i++}:Base Price is not In Correct Format");
                    }
                    if (decimal.TryParse(dict["weight_kg"].ToString(), out var weightKg))
                    {
                        if (weightKg <= 0)
                        {
                            throw new Exception($"{i++}:Weight is not postitive");
                        }
                    }
                    else
                    {
                        throw new Exception($"{i++}:Weight is not In Correct Format");
                    }

                    var productid = await _productRepository.UpsertProductAsync(dict).ConfigureAwait(false);

                    string shippingClass = string.IsNullOrWhiteSpace(dict["shipping_class"].ToString()) ? "Standard" : dict["shipping_class"].ToString()!;
                    string brandName = dict["brand_name"].ToString()!;
                    string categoryPath = dict["category_path"].ToString()!;


                    await _categoryRepository.InsertCategoryAsync(categoryPath, productid).ConfigureAwait(false);
                    await _brandRepository.InsertBrandAsync(brandName, productid).ConfigureAwait(false);
                    await _shippingRepository.InsertShippingClassAsync(shippingClass, productid).ConfigureAwait(false);
                    await _variantRepository.SyncVariantAsync(dict, productid).ConfigureAwait(false);
                    await _inventoryRepository.SyncInventoryAsync(dict, productid).ConfigureAwait(false);
                    await _imageService.InsertImagesAsync(dict, productid).ConfigureAwait(false);

                    summary.SuccessCount++;
                    summary.Messages.Add($"{i++}:Record Saved Successfully!");

                }
                catch (Exception e)
                {
                    summary.Messages.Add(e.Message);
                }
            });


        }
        catch (Exception e)
        {
            summary.Messages.Add(e.Message);
        }
        return summary;
    }

    private static bool IsValidDimention(string? value)
    {
        var dimensions = value?.Split("x");
        if (dimensions == null || dimensions.Length != 3) return false;

        foreach (var dimension in dimensions)
        {
            if (!decimal.TryParse(dimension, out var a)) return false;
        }
        return true;

    }
}
