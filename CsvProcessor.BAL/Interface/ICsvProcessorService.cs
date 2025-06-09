using CsvProcessor.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace CsvProcessor.BAL.Interface;

public interface ICsvProcessorService
{
    public Task<ImportSummaryDto> ProcessCsvAsync(IFormFile file);

}
