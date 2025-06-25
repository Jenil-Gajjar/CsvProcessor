using System.Text.Json;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.Constants;
using CsvProcessor.Models.DTOs;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class ShippingRepository : IShippingRepository
{
    private readonly string _conn;
    public ShippingRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString(Constants.MyConnectionString)!;
    }


    public async Task BulkInsertShippingClassAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();
        foreach (var record in records)
        {
            if (!SkuIdDict.TryGetValue(record[Constants.product_sku].ToString()?.ToLower()!, out var id)) continue;
            string? shipping_class = record[Constants.shipping_class].ToString()?.Trim().ToLower();
            dataList.Add(new
            {
                product_id = id,
                shipping_class
            });
        }
        var jsonData = JsonSerializer.Serialize(dataList);
        await conn.ExecuteAsync("select * from fn_shipping_class_bulk_insert(@data::jsonb)", new { data = jsonData });

    }
}


