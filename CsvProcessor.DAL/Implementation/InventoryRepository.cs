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
    public async Task SyncInventoryAsync(IDictionary<string, object> dict, int productid)
    {
        using var conn = new NpgsqlConnection(_conn);
        foreach (var kv in dict)
        {
            if (kv.Key.StartsWith("warehouse_"))
            {
                var warehouseValue = kv.Key.Replace("warehouse_", "").Replace("_stock", "");

                int warehouseid = await conn.ExecuteScalarAsync<int>("SELECT fn_warehouse_insert(@p_warehouse_name)", new { p_warehouse_name = warehouseValue });

                int stock = int.TryParse(kv.Value.ToString(), out var result) ? result : 0;
                await conn.ExecuteAsync("SELECT fn_inventory_upsert(@p_product_id,@p_warehouse_id,@p_stock_level)", new
                {
                    p_product_id = productid,
                    p_warehouse_id = warehouseid,
                    p_stock_level = stock
                });
            }
        }
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
