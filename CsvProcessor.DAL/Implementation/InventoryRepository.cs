using System.Text.Json;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.Constants;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class InventoryRepository : IInventoryRepository
{

    private readonly string _conn;

    public InventoryRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString(Constants.MyConnectionString)!;

    }

    public async Task<int> BulkInsertInventoryAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();

        foreach (var record in records)
        {
            foreach (var kv in record)
            {
                if (kv.Key.StartsWith(Constants.warehouse_))
                {
                    if (!SkuIdDict.TryGetValue(record[Constants.product_sku].ToString()?.ToLower() ?? "", out var product_id)) continue;

                    var warehouseValue = kv.Key.Replace(Constants.warehouse_, "").Replace(Constants._stock, "").Trim();
                    if (string.IsNullOrWhiteSpace(warehouseValue)) continue;
                    int stock = int.TryParse(kv.Value.ToString(), out var result) ? result : 0;
                    dataList.Add(new
                    {
                        product_id,
                        warehouse = warehouseValue,
                        stock_level = stock
                    });
                }
            }
        }
        var jsonData = JsonSerializer.Serialize(dataList);
        var updateCount = await conn.ExecuteScalarAsync<int>("select fn_warehouse_inventory_bulk_insert(@data::jsonb)", new { data = jsonData });
        return updateCount;
    }
}
