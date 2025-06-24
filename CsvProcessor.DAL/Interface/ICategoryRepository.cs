using CsvProcessor.Models.DTOs;

namespace CsvProcessor.DAL.Interface;

public interface ICategoryRepository
{
    public Task BulkInsertCategoryAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict, ImportSummaryDto summary);

}
