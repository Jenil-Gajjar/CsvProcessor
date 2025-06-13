using System.Text.Json;
using CsvProcessor.DAL.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class CategoryRepository : ICategoryRepository
{
    private readonly string _conn;
    public CategoryRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
    }


    public async Task BulkInsertCategoryAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {

        using var conn = new NpgsqlConnection(_conn);

        var rawData = records.Where(kv => !string.IsNullOrEmpty(kv["category_path"].ToString())).Select(kv => new
        {
            product_id = SkuIdDict.TryGetValue(kv["product_sku"].ToString()!, out var id) ? id : 0,
            category_path = kv["category_path"]
        });

        var jsonData = JsonSerializer.Serialize(rawData);

        await conn.ExecuteAsync("select fn_category_bulk_insert(@data::jsonb)", new { data = jsonData });
    }
}
