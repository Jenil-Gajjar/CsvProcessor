using System.Diagnostics;
using System.Text;
using CsvProcessor.BAL.Interface;
using Microsoft.AspNetCore.Mvc;
namespace CsvProcessor.Web.Controllers;
public class CsvProcessorController : Controller
{
    private readonly ICsvProcessorService _csvProcessorService;
    public CsvProcessorController(ICsvProcessorService csvProcessorService) => _csvProcessorService = csvProcessorService;
    public IActionResult Index() => View();

    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ViewBag.Message = "Please Select a CSV file";
            return View(nameof(Index));
        }
        Stopwatch stopwatch = Stopwatch.StartNew();
        var summary = await _csvProcessorService.ProcessCsvAsync(file);
        stopwatch.Stop();

        var content = new StringBuilder();
        content.AppendLine($"Total Time Taken:{stopwatch.Elapsed}");
        content.AppendLine($"Total Records:{summary.RowCount}");
        content.AppendLine($"Total Inserted Records:{summary.InsertedRecords}");
        content.AppendLine($"Total Updated Records:{summary.UpdatedRecords}");
        content.AppendLine($"Total Skipped Records:{summary.RowCount - summary.InsertedRecords - summary.UpdatedRecords}");
        content.AppendLine($"Total Url Successfully Processed:{summary.TotalSuccessfullUrls}");
        if (summary.Warnings.Any())
        {
            content.AppendLine("\nWarnings:");
            summary.Warnings.ForEach(e => content.AppendLine(e));
        }
        if (summary.Errors.Any())
        {
            content.AppendLine("\nErrors:");
            summary.Errors.ForEach(e => content.AppendLine(e));
        }
        var fileName = Path.GetFileNameWithoutExtension(file.FileName);

        return File(Encoding.UTF8.GetBytes(content.ToString()), "text/plain", $"{fileName}_report.txt");
    }
}
