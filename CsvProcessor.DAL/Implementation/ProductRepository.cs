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

    public async Task<ProductDto> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records)
    {
        var dataList = new List<object>();

        foreach (var record in records)
        {
            string? status = record["status"].ToString();
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Active";
            }
            dataList.Add(new
            {
                product_sku = record["product_sku"],
                product_name = record["product_name"],
                description = record["description"],
                base_price = record["base_price"],
                supplier_sku = record["supplier_sku"],
                weight_kg = record["weight_kg"],
                dimensions_cm = record["dimensions_cm"],
                status,
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
}
