using System.Text.Json;
using CsvProcessor.DAL.Interface;
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
 
    public async Task BulkInsertBrandAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {

        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();

        var rawData = records.Where(kv => !string.IsNullOrEmpty(kv["brand_name"].ToString())).Select(kv => new
        {
            product_id = SkuIdDict.TryGetValue(kv["product_sku"].ToString()!, out var id) ? id : 0,
            brand_name = kv["brand_name"]
        });

        var jsonData = JsonSerializer.Serialize(rawData);
        await conn.ExecuteAsync("select fn_brand_bulk_insert(@data::jsonb)", new { data = jsonData });

    }
}
