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
}
