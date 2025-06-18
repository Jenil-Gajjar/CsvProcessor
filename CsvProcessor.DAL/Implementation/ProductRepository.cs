using System.Text.Json;
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

    public async Task<(Dictionary<string, int>, Dictionary<string, int>)> BulkUpsertProductAsync(IEnumerable<Dictionary<string, object>> records)
    {
        var dataList = new List<object>();

        records.ToList().ForEach(record =>
        {
            if (string.IsNullOrWhiteSpace(record["status"].ToString()))
            {
                record["status"] = "Active";
            }
            dataList.Add(new
            {
                product_sku = record["product_sku"],
                product_name = record["product_name"],
                description = record["description"],
                base_price = record["base_price"],
                supplier_sku = record["supplier_sku"],
                weight_kg = record["weight_kg"],
                dimensions_cm = record["dimensions_cm"],
                status = record["status"],
            });
        });

        string jsonData = JsonSerializer.Serialize(dataList);
        try
        {
            using var conn = new NpgsqlConnection(_conn);
            var result = await conn.QueryAsync<(int id, string Sku, bool IsInserted)>("SELECT * from public.fn_product_bulk_upsert(@data::jsonb)", new { data = jsonData });
            Dictionary<string, int> recordCounts = new()
            {
                {"InsertedRecords" ,result.Count(u => u.IsInserted)},
                {"UpdatedRecords" ,result.Count(u => !u.IsInserted)},
            };
            return (result.ToDictionary(t => t.Sku, t => t.id), recordCounts);
        }
        catch (Exception e)
        {

            Console.WriteLine(e.Message);
            Console.WriteLine("Error While Saving Data to Db");
            throw;
        }

    }
}
