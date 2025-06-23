using System.Text.Json;
using CsvProcessor.DAL.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class ShippingRepository : IShippingRepository
{
    private readonly string _conn;
    public ShippingRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
    }


    public async Task<List<string>> BulkInsertShippingClassAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();
        List<string> warnings = new();
        foreach (var record in records)
        {
            if (!SkuIdDict.TryGetValue(record["product_sku"].ToString()?.ToLower()!, out var id)) continue;
            string? shipping_class = record["shipping_class"].ToString();
            if (string.IsNullOrWhiteSpace(shipping_class))
            {
                shipping_class = "standard";
                warnings.Add($"{record["product_sku"]}:Invalid shipping class defaulted to 'standard'");
            }
            dataList.Add(new
            {
                product_id = id,
                shipping_class = shipping_class.ToString().Trim().ToLower()
            });
        }
        var jsonData = JsonSerializer.Serialize(dataList);
        await conn.ExecuteAsync("select * from fn_shipping_class_bulk_insert(@data::jsonb)", new { data = jsonData });
        return warnings;
    }
}


