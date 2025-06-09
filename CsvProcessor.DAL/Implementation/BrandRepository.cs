using CsvProcessor.DAL.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class BrandRepository : IBrandRepository
{
    private readonly string _conn;
    public BrandRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;

    }
    public async Task InsertBrandAsync(string brandName, int productid)
    {
        using var conn = new NpgsqlConnection(_conn);
        int brandid = await conn.ExecuteScalarAsync<int>("SELECT fn_brand_insert(@p_brand_name)", new { p_brand_name = brandName });
        await conn.ExecuteAsync("UPDATE products SET brand_id=@brandid where product_id = @productid", new { brandid, productid });
    }
}
