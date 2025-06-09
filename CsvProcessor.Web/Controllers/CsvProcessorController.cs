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
        var report = await _csvProcessorService.ProcessCsvAsync(file);
        stopwatch.Stop();

        var content = new StringBuilder();
        content.AppendLine($"Total Time Taken:{stopwatch.Elapsed}");
        
        report.Messages.ForEach(e => content.AppendLine(e));
        var fileName = Path.GetFileNameWithoutExtension(file.FileName);

        return File(Encoding.UTF8.GetBytes(content.ToString()), "text/plain", $"{fileName}_Report.txt");
    }
}
