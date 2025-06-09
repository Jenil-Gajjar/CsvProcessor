using Microsoft.Extensions.Configuration;
using CsvProcessor.DAL.Interface;
using Dapper;
using Npgsql;

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
}
