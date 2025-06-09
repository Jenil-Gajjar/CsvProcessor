using CsvProcessor.DAL.Interface;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class CategoryRepository : ICategoryRepository
{
    private readonly string _conn;
    public CategoryRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
    }

    public async Task InsertCategoryAsync(string path, int productid)
    {
        using var conn = new NpgsqlConnection(_conn);

        int categoryid = await conn.ExecuteScalarAsync<int>("SELECT fn_category_insert(@p_path)", new { p_path = path });

        await conn.ExecuteAsync("UPDATE products SET category_id=@categoryid where product_id=@productid", new { categoryid, productid });

    }

}
