using System.Text.Json;
using CsvProcessor.DAL.Interface;
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
        _conn = configuration.GetConnectionString("MyConnectionString")!;
        _logger = logger;
    }

    public async Task<ProductDto> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records, ImportSummaryDto summary)
    {
        var dataList = new List<object>();
        foreach (var record in records)
        {
            dataList.Add(new
            {
                product_sku = GetString(record, "product_sku"),
                product_name = GetString(record, "product_name"),
                description = GetString(record, "description"),
                base_price = GetString(record, "base_price"),
                supplier_sku = GetString(record, "supplier_sku"),
                weight_kg = GetString(record, "weight_kg"),
                dimensions_cm = GetString(record, "dimensions_cm"),
                status = GetString(record, "status"),
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
