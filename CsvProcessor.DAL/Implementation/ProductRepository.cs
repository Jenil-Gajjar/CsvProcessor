using CsvProcessor.DAL.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class ProductRepository : IProductRepository
{
    private readonly string _conn;
    public ProductRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
    }

    public async Task<int> UpsertProductAsync(IDictionary<string, object> dict)
    {
        using var conn = new NpgsqlConnection(_conn);

        var query = "SELECT fn_product_upsert(@p_sku,@p_name,@p_description,@p_base_price,@p_supplier_sku,@p_weight_kg,@p_dimensions_cm,@p_status)";
        var parameters = new
        {
            p_sku = dict["product_sku"],
            p_name = dict["product_name"],
            p_description = dict["description"],
            p_base_price = decimal.TryParse(dict["base_price"].ToString(), out var bp) ? bp : 0m,
            p_supplier_sku = dict["supplier_sku"],
            p_weight_kg = decimal.TryParse(dict["weight_kg"].ToString(), out var wk) ? wk : 0m,
            p_dimensions_cm = dict["dimensions_cm"],
            p_status = dict["status"],
        };
        return await conn.ExecuteScalarAsync<int>(query, parameters);
    }

}
