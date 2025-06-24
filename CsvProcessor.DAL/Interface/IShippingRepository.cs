using CsvProcessor.Models.DTOs;

namespace CsvProcessor.DAL.Interface;

public interface IShippingRepository

{
    public Task BulkInsertShippingClassAsync(IEnumerable<IDictionary<string, object>> records, IDictionary<string, int> SkuIdDict, ImportSummaryDto summary);

}
