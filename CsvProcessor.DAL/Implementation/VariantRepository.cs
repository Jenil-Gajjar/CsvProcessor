using Microsoft.Extensions.Configuration;
using CsvProcessor.DAL.Interface;
using Dapper;
using Npgsql;
using System.Text.Json;
using CsvProcessor.Models.Constants;

namespace CsvProcessor.DAL.Implementation;

public class VariantRepository : IVariantRepository
{
    private readonly string _conn;

    public VariantRepository(IConfiguration configuration)
    {

        _conn = configuration.GetConnectionString(Constants.MyConnectionString)!;

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
                if (kv.Key.StartsWith(Constants.variant_type_))
                {
                    if (!SkuIdDict.TryGetValue(record[Constants.product_sku].ToString()?.ToLower() ?? "", out var product_id)) continue;

                    var suffix = kv.Key.Replace(Constants.variant_type_, "");
                    var variantValueKey = $"${Constants.variant_value_}{suffix}";
                    var type = kv.Value.ToString();
                    if (!record.TryGetValue(variantValueKey, out var value)) continue;

                    if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        dataList.Add(new
                        {
                            product_id,
                            variant_type = type.ToString().Trim().ToLower(),
                            variant_value = value.ToString()?.Trim().ToLower()
                        });
                    }
                }
            }
        }

        var jsonData = JsonSerializer.Serialize(dataList);

        await conn.ExecuteAsync("select fn_variant_bulk_upsert(@data::jsonb)", new { data = jsonData });

    }
}
