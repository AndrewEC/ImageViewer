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

    private const int ThumbnailTimeToLiveMinutes = 5;
    private const int ImageTimeToLiveMinutes = 3;
    private const int TargetWidth = 200;

    private static readonly string ThumbnailKeyTemplate = "thumbnail-{0}";
    private static readonly string ImageKeyTemplate = "image-{0}";

    private readonly ConsoleLogger<ThumbnailCache> logger = new();

    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    private ThumbnailCache() { }

    /// <summary>
    /// Loads a thumbnail from the specified path. This will return a previously
    /// loaded and cached image if one is available.
    /// </summary>
    /// <param name="imagePath">The absolute path to the image being loaded.</param>
    /// <returns>The async task for loading the thumbnail bitmap.</returns>
    public Task<Bitmap?> LoadThumbnail(string? imagePath) => DoLoadImage(
        string.Format(ThumbnailKeyTemplate, imagePath),
        ThumbnailTimeToLiveMinutes,
        () => ReadThumbnail(imagePath));

    /// <summary>
    /// Loads an image from the specified path. This will return a previously
    /// loaded and cached image if one is available.
    /// </summary>
    /// <param name="imagePath">The absolute path to the image being loaded.</param>
    /// <returns>An async task for loading the image bitmap.</returns>
    public Task<Bitmap?> LoadImage(string? imagePath) => DoLoadImage(
        string.Format(ImageKeyTemplate, imagePath),
        ImageTimeToLiveMinutes,
        () => ReadImage(imagePath));

    private static Bitmap? ReadThumbnail(string? path)
    {
        if (path == null)
        {
            return null;
        }

        using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            return Bitmap.DecodeToWidth(stream, TargetWidth, BitmapInterpolationMode.LowQuality);
        }
    }

    private static Bitmap? ReadImage(string? path)
    {
        if (path == null)
        {
            return null;
        }

        return new(path);
    }

    private Task<Bitmap?> DoLoadImage(string key, int timeToLive, Func<Bitmap?> loaderFunction) => Task.Run(() =>
    {
        if (cache.TryGetValue(key, out object? cachedThumbnail))
        {
            return (Bitmap?)cachedThumbnail;
        }

        logger.Log($"Attempting to load bitmap for key [{key}].");
        Bitmap? bitmap = loaderFunction.Invoke();

        if (bitmap == null)
        {
            return null;
        }

        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(timeToLive))
            .RegisterPostEvictionCallback(OnEntryEvicted);

        cache.Set(key, bitmap, options);

        return bitmap;
    });

    private void OnEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        logger.Log($"Image entry [{key}] was evicted from cache with reason [{reason}].");

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