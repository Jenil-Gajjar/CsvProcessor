using System.Collections.Concurrent;

namespace CsvProcessor.BAL.Helper;

public static class ThumbnailQueue
{

    public static readonly ConcurrentQueue<string> thumbnailQueue = new();

}
