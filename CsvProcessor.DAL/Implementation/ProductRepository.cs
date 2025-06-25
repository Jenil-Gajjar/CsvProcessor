using System.Text.Json;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.Constants;
using CsvProcessor.Models.DTOs;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class ProductRepository : IProductRepository
{
    private readonly string _conn;

    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(IConfiguration configuration, ILogger<ProductRepository> logger)
    {
        _conn = configuration.GetConnectionString(Constants.MyConnectionString)!;
        _logger = logger;
    }

    public async Task<ProductDto> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records, ImportSummaryDto summary)
    {
        var dataList = new List<object>();
        foreach (var record in records)
        {
            dataList.Add(new
            {
                product_sku = GetString(record, Constants.product_sku),
                product_name = GetString(record, Constants.product_name),
                description = GetString(record, Constants.description),
                base_price = GetString(record, Constants.base_price),
                supplier_sku = GetString(record, Constants.supplier_sku),
                weight_kg = GetString(record, Constants.weight_kg),
                dimensions_cm = GetString(record, Constants.dimensions_cm),
                status = GetString(record, Constants.status),
            });
        }

        string jsonData = JsonSerializer.Serialize(dataList);
        try
        {
            using var conn = new NpgsqlConnection(_conn);
            var result = await conn.QueryAsync<(int id, string Sku, bool IsInserted)>("SELECT * from public.fn_product_bulk_upsert(@data::jsonb)", new { data = jsonData });

            ProductDto productDto = new()
            {
                SkuToIdDict = result.ToDictionary(t => t.Sku, t => t.id),
                InsertedRecords = result.Count(u => u.IsInserted),
                UpdatedRecords = result.Count(u => !u.IsInserted),
            };

            return productDto;
        }
        catch (Exception e)
        {

            _logger.LogError("{Message}", e.Message);
            throw;
        }
    }

    public static string? GetString(Dictionary<string, object> dict, string key)
    {
        return dict[key].ToString()?.Trim().ToLower();
    }
}
