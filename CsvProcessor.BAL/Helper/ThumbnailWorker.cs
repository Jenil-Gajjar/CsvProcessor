namespace CsvProcessor.BAL.Helper;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// public class ThumbnailWorker : BackgroundService
// {
//     private readonly static string _imageDir = Path.Combine("wwwroot", "images");

//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {


//         while (!stoppingToken.IsCancellationRequested)
//         {
//             while (ThumbnailQueue.thumbnailQueue.TryDequeue(out var filePath))
//             {
//                 using var image = Image.Load(filePath);
//                 foreach (var size in new[] { ("thumb", 150), ("medium", 600) })
//                 {

//                     int width = size.Item2;
//                     string suffix = size.Item1;

//                     var ratio = width / image.Width;
//                     int height = image.Height * ratio;

//                     image.Mutate(x => x.Resize(new ResizeOptions
//                     {
//                         Size = new Size(width, height),
//                         Mode = ResizeMode.Max
//                     }));
//                     string ext = Path.GetExtension(filePath);
//                     string fileName = Path.GetFileNameWithoutExtension(filePath);
//                     string output = Path.Combine(_imageDir, $"{fileName}_{suffix}.{ext}");
//                     image.Save(output);
//                 }
//             }
//             await Task.Delay(1000, stoppingToken);
//         }

//     }
// }
