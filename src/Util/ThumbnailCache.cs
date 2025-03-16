namespace ImageViewer.Util;

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ImageViewer.Log;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// A singleton utility class to help load and cache thumbnail images.
/// All images will be cached by their patch and for up to 5 minutes.
/// All images are automatically decoded to a target width of 200 pixels
/// wide.
/// </summary>
public sealed class ThumbnailCache
{
    /// <summary>
    /// The singleton instance of the <see cref="ThumbnailCache"/>.
    /// </summary>
    public static readonly ThumbnailCache Instance = new();

    private const int TargetWidth = 200;

    private readonly ConsoleLogger<ThumbnailCache> logger = new();

    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    private ThumbnailCache() { }

    /// <summary>
    /// Loads a single thumbnail from the specified path. This will first check
    /// if a thumbnail for this path has been previously cached and return
    /// it if it has not yet expired.
    /// </summary>
    /// <param name="path">The absolute path to the image being loaded.</param>
    /// <returns>The async task for loading the thumbnail bitmap.</returns>
    public Task<Bitmap> LoadThumbnail(string path) => Task.Run(() =>
    {
        if (cache.TryGetValue(path, out object? cachedThumbnail))
        {
            return (Bitmap)cachedThumbnail!;
        }

        logger.Log($"Creating and caching thumbnail from [{path}].");
        Bitmap thumbnail = ReadFresh(path);

        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .RegisterPostEvictionCallback(OnEntryEvicted);

        cache.Set(path, thumbnail, options);

        return thumbnail;
    });

    private static Bitmap ReadFresh(string path)
    {
        using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            return Bitmap.DecodeToWidth(stream, TargetWidth, BitmapInterpolationMode.LowQuality);
        }
    }

    private void OnEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        logger.Log($"Thumbnail entry [{key}] was evicted from cache with reason [{reason}].");

        if (reason == EvictionReason.Replaced)
        {
            return;
        }

        if (value is Bitmap bitmap)
        {
            try
            {
                bitmap.Dispose();
            }
            catch (Exception e)
            {
                logger.Error("Failed to dispose evicted bitmap.", e);
            }
        }
    }
}