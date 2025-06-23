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
        var dataList = new List<object>();

        foreach (var record in records.Where(kv => !string.IsNullOrEmpty(kv["category_path"].ToString())))
        {
            if (!SkuIdDict.TryGetValue(record["product_sku"].ToString()?.ToLower()!, out var id)) continue;
            dataList.Add(new
            {
                product_id = id,
                category_path = record["category_path"].ToString()?.Trim()
            });
        }


        var jsonData = JsonSerializer.Serialize(dataList);

        await conn.ExecuteAsync("select fn_category_bulk_insert(@data::jsonb)", new { data = jsonData });
    }
}
