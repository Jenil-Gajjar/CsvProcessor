using System.Collections.Concurrent;
using System.Text.Json;
using CsvProcessor.DAL.Interface;
using CsvProcessor.Models.DTOs;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvProcessor.DAL.Implementation;

public class ProductImageRepository : IProductImageRepository
{
    private readonly string _conn;
    public ProductImageRepository(IConfiguration configuration)
    {
        _conn = configuration.GetConnectionString("MyConnectionString")!;
    }

    public async Task BulkInsertImagesAsync(ConcurrentBag<ProductImageDto>? set)
    {
        if (set == null || set.IsEmpty) return;

        using var conn = new NpgsqlConnection(_conn);
        var jsonData = JsonSerializer.Serialize(set);
        await conn.ExecuteAsync("select fn_product_image_bulk_insert(@data::jsonb)", new { data = jsonData });
    }
}
