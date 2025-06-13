using System.Security.Cryptography.X509Certificates;
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
 

    public async Task BulkInsertShippingClassAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);

        var rawData = records.Where(kv => !string.IsNullOrEmpty(kv["shipping_class"].ToString())).Select(kv => new
        {
            product_id = SkuIdDict.TryGetValue(kv["product_sku"].ToString()!, out var id) ? id : 0,
            shipping_class = kv["shipping_class"]
        });

        var jsonData = JsonSerializer.Serialize(rawData);
        await conn.ExecuteAsync("select fn_shipping_class_bulk_insert(@data::jsonb)", new { data = jsonData });

    }
}


