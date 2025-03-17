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
public sealed class ImageCache
{
    /// <summary>
    /// The singleton instance of the <see cref="ImageCache"/>.
    /// </summary>
    public static readonly ImageCache Instance = new();

    private const int TargetWidth = 200;

    private static readonly object Mutex = new();
    private static readonly string ThumbnailCacheKeyTemplate = "thumbnail-{0}";
    private static readonly string ImageCacheKeyTemplate = "image-{0}";

    private readonly ConsoleLogger<ImageCache> logger = new();

    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    private ImageCache() { }

    /// <summary>
    /// Loads a thumbnail from the specified path. This will return a previously
    /// loaded and cached image if one is available.
    /// </summary>
    /// <param name="imagePath">The absolute path to the image being loaded.</param>
    /// <returns>The async task for loading the thumbnail bitmap.</returns>
    public Task<Bitmap?> LoadThumbnail(string? imagePath)
        => DoLoadImage(
            string.Format(ThumbnailCacheKeyTemplate, imagePath),
            imagePath,
            ReadThumbnail);

    /// <summary>
    /// Loads an image from the specified path. This will return a previously
    /// loaded and cached image if one is available.
    /// </summary>
    /// <param name="imagePath">The absolute path to the image being loaded.</param>
    /// <returns>An async task for loading the image bitmap.</returns>
    public Task<Bitmap?> LoadImage(string? imagePath) => DoLoadImage(
        string.Format(ImageCacheKeyTemplate, imagePath),
        imagePath,
        ReadImage);

    private static Bitmap ReadThumbnail(string path)
    {
        using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            return Bitmap.DecodeToWidth(stream, TargetWidth, BitmapInterpolationMode.LowQuality);
        }
    }

    private static Bitmap ReadImage(string path) => new(path);

    private Task<Bitmap?> DoLoadImage(string key, string? imagePath, Func<string, Bitmap> loaderFunction)
        => Task.Run(() =>
        {
            if (imagePath == null)
            {
                return null;
            }

            if (cache.TryGetValue(key, out object? cachedThumbnail))
            {
                return (Bitmap?)cachedThumbnail;
            }

            logger.Log($"Attempting to load bitmap for key [{key}].");
            Bitmap bitmap = loaderFunction.Invoke(imagePath);

            lock (Mutex)
            {
                if (cache.TryGetValue(key, out object? cachedThumbnail2))
                {
                    logger.Log($"Secondary cache lookup returned result for key [{key}]. Possible concurrency issue.");
                    bitmap.Dispose();
                    return (Bitmap?)cachedThumbnail2;
                }

                cache.Set(key, bitmap);
            }

            return bitmap;
        });
}