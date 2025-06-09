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

    public CsvProcessorService(IProductRepository productRepository, ICategoryRepository categoryRepository, IBrandRepository brandRepository, IVariantRepository variantRepository, IShippingRepository shippingRepository, IInventoryRepository inventoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _variantRepository = variantRepository;
        _shippingRepository = shippingRepository;
        _inventoryRepository = inventoryRepository;
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
            for (int i = 0; i < records.Count; i++)
            {
                var dict = records[i];
                try
                {
                    var dimensions = dict["dimensions_cm"].ToString();
                    if (!IsValidDimention(dimensions)) throw new Exception($"{i + 1}:Invalid dimensions. Use the format LxWxH");

                    var hasImage = dict.Keys.Any(k => k.StartsWith("image_url") && !string.IsNullOrWhiteSpace(dict[k]?.ToString()));

                    if (!hasImage) throw new Exception($"{i + 1}:At least one image is required");

                    var productid = await _productRepository.UpsertProductAsync(dict);

                    await _categoryRepository.InsertCategoryAsync(dict["category_path"].ToString()!, productid);
                    await _brandRepository.InsertBrandAsync(dict["brand_name"].ToString()!, productid);
                    await _shippingRepository.InsertShippingClassAsync(dict["shipping_class"].ToString()!, productid);
                    await _variantRepository.SyncVariantAsync(dict, productid);
                    await _inventoryRepository.SyncInventoryAsync(dict, productid);

                    summary.SuccessCount++;
                    summary.Messages.Add($"{i + 1}:Record Saved Successfully!");
                }
                catch (Exception e)
                {
                    summary.Messages.Add(e.Message);
                }
            }

        }
        catch (Exception e)
        {
            summary.Messages.Add(e.Message);
        }
        return summary;
    }

    private bool IsValidDimention(string? value) => Regex.IsMatch(value ?? "", @"^\d+x\d+x\d+$");
}
