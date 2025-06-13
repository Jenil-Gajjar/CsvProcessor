using System.Text.Json;
using CsvProcessor.DAL.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class InventoryRepository : IInventoryRepository
{

    private readonly string _conn;

    public InventoryRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;

    }

    public async Task BulkInsertInventoryAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict)
    {
        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();

        foreach (var record in records)
        {
            foreach (var kv in record)
            {
                if (kv.Key.StartsWith("warehouse_"))
                {
                    if (!SkuIdDict.TryGetValue(record["product_sku"].ToString() ?? "", out var product_id)) continue;

                    var warehouseValue = kv.Key.Replace("warehouse_", "").Replace("_stock", "");
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
        await conn.ExecuteAsync("select fn_warehouse_inventory_bulk_insert(@data::jsonb)", new { data = jsonData });
    }
}
