using System.Text.Json;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.DTOs;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class BrandRepository : IBrandRepository
{
    private readonly string _conn;

    public BrandRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
    }

    public async Task BulkInsertBrandAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict, ImportSummaryDto summary)
    {

        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();

        foreach (var record in records.Where(kv => !string.IsNullOrEmpty(kv["brand_name"].ToString())))
        {
            if (!SkuIdDict.TryGetValue(record["product_sku"].ToString()?.ToLower()!, out var id)) continue;
            dataList.Add(new
            {
                product_id = id,
                brand_name = record["brand_name"].ToString()?.Trim().ToLower()
            });
        }

        var jsonData = JsonSerializer.Serialize(dataList);
        var brandNames = await conn.QueryAsync<string>("select * from fn_brand_bulk_insert(@data::jsonb)", new { data = jsonData });
        foreach (var brandName in brandNames)
        {
            summary.Information["Brand"].Add($"Brand {brandName} is created");
        }
    }
}
