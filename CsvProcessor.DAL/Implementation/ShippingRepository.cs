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
    public async Task InsertShippingClassAsync(string className, int productid)
    {
        using var conn = new NpgsqlConnection(_conn);

        int shippingClassId = await conn.ExecuteScalarAsync<int>("SELECT fn_shipping_class_insert(@p_class_name)", new { p_class_name = className });
        await conn.ExecuteAsync("UPDATE products SET shipping_class_id=@shippingClassId WHERE product_id=@productid", new { shippingClassId, productid });
    }

    public async Task BulkInsertShippingClassAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);

        var rawData = records.Where(kv => !string.IsNullOrEmpty(kv["shipping_class"].ToString())).Select(kv => new
        {
            product_id = SkuIdDict.TryGetValue("product_sku", out var id) ? id : 0,
            shipping_class = kv["shipping_class"]
        });

        var jsonData = JsonSerializer.Serialize(rawData);
        await conn.ExecuteAsync("select fn_shipping_class_bulk_insert(@data::jsonb)", new { data = jsonData });

    }
}


