using Microsoft.Extensions.Configuration;
using CsvProcessor.DAL.Interface;
using Dapper;
using Npgsql;
using System.Text.Json;

namespace CsvProcessor.DAL.Implementation;

public class VariantRepository : IVariantRepository
{
    private readonly string _conn;

    public VariantRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;

    }
    public async Task SyncVariantAsync(IDictionary<string, object> dict, int productid)
    {
        using var conn = new NpgsqlConnection(_conn);
        await conn.ExecuteAsync("DELETE FROM product_variants WHERE product_id = @productid", new { productid });

        foreach (var kv in dict)
        {
            if (kv.Key.StartsWith("variant_type"))
            {
                var suffix = kv.Key.ToString().Replace("variant_type", "");
                var variantType = kv.Value.ToString();
                if (string.IsNullOrWhiteSpace(variantType)) continue;
                var variantValueKey = "variant_value" + suffix;
                if (!dict.TryGetValue(variantValueKey, out var variantValue)) continue;

                int variantTypeId = await conn.ExecuteScalarAsync<int>("SELECT public.fn_variant_type_upsert(@p_type_name)", new { p_type_name = variantType });

                await conn.ExecuteAsync("INSERT INTO public.product_variants(product_id, variant_type_id, variant_value) Values(@productid,@variantTypeId,@variantValue)", new { productid, variantTypeId, variantValue });
            }
        }
    }


    public async Task BulkInsertVariantAsync(IEnumerable<IDictionary<string, object>> records,
    IDictionary<string, int> SkuIdDict
    )
    {
        using var conn = new NpgsqlConnection(_conn);
        var dataList = new List<object>();
        foreach (var record in records)
        {
            foreach (var kv in record)
            {
                if (kv.Key.StartsWith("variant_type_"))
                {
                    if (!SkuIdDict.TryGetValue(record["product_sku"].ToString() ?? "", out var product_id)) continue;

                    var suffix = kv.Key.Replace("variant_type_", "");
                    var variantValueKey = $"variant_value_{suffix}";
                    var type = kv.Value.ToString();
                    if (!record.TryGetValue(variantValueKey, out var value)) continue;

                    if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        dataList.Add(new
                        {
                            product_id,
                            variant_type = type,
                            variant_value = value
                        });
                    }
                }
            }
        }

        var jsonData = JsonSerializer.Serialize(dataList);

        await conn.ExecuteAsync("select fn_inventory_bulk_upsert(@data::jsonb)", new { data = jsonData });

    }
}
