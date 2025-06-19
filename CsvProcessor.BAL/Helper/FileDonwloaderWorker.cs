namespace CsvProcessor.BAL.Helper;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public class FileDonwloaderWorker : BackgroundService
{
    private readonly static string _imageDir = Path.Combine("wwwroot", "images");

    private readonly ILogger<FileDonwloaderWorker> _logger;

    public FileDonwloaderWorker(ILogger<FileDonwloaderWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            while (ImageProcessingQueue.ImageQueue.TryDequeue(out var imageProcessDto))
            {
                string filePath = imageProcessDto.ImagePath;
                if (imageProcessDto.ResponseContent == null) continue;
                byte[] imageBytes = await imageProcessDto.ResponseContent.ReadAsByteArrayAsync(stoppingToken);
                if (imageBytes == null) continue;
                try
                {
                    await File.WriteAllBytesAsync(imageProcessDto.ImagePath, imageBytes, stoppingToken);


                    using var image = Image.Load(filePath);
                    foreach (var size in new[] { ("thumb", 150), ("medium", 600) })
                    {
                        int width = size.Item2;
                        string suffix = size.Item1;

                        double ratio = (double)width / image.Width;
                        int height = (int)(image.Height * ratio);

                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = ResizeMode.Max
                        }));
                        string ext = Path.GetExtension(filePath);
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string output = Path.Combine(_imageDir, $"{fileName}_{suffix}.{ext}");
                        image.Save(output);

                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("{Message}", e.Message);
                }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
