namespace ImageViewer.Util;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Media.Imaging;
using ImageViewer.Log;
using ImageViewer.Models;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// A singleton utility class to help load and cache images.
/// All images will be cached by their patch and for up to 5 minutes.
/// All images are automatically decoded to a target width of 200 pixels
/// wide.
/// </summary>
public sealed class ImageCache : IDisposable
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
    private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
    private readonly HashSet<string> knownCacheKeys = [];
    private readonly Timer timer;

    private readonly Bitmap defaultBitmap;

    private bool disposed = false;

    private ImageCache()
    {
        defaultBitmap = new("./Assets/default.png");

        timer = new Timer(TimeSpan.FromMinutes(10));
        timer.Elapsed += OnTimerTick;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    /// <summary>
    /// Loads a thumbnail from the specified path. This will return a previously
    /// loaded and cached image if one is available.
    /// </summary>
    /// <param name="imagePath">The absolute path to the image being loaded.</param>
    /// <returns>The async task for loading the thumbnail bitmap.</returns>
    public Task<Bitmap> LoadThumbnailAsync(PathLike? imagePath)
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
    public Task<Bitmap> LoadImageAsync(PathLike imagePath)
        => DoLoadImage(
            string.Format(ImageCacheKeyTemplate, imagePath),
            imagePath,
            ReadImage);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (Mutex)
        {
            RunMany(
                cache.Clear,
                cache.Dispose,
                defaultBitmap.Dispose,
                () =>
                {
                    timer.Enabled = false;
                    timer.Dispose();
                });
        }

        disposed = true;
        GC.SuppressFinalize(this);
    }

    private static void RunMany(params Action[] actions)
    {
        foreach (Action action in actions)
        {
            try
            {
                action.Invoke();
            }
            catch
            {
            }
        }
    }

    private static Bitmap ReadThumbnail(string path)
    {
        using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            return Bitmap.DecodeToWidth(stream, TargetWidth, BitmapInterpolationMode.LowQuality);
        }
    }

    private static Bitmap ReadImage(string path) => new(path);

    private Task<Bitmap> DoLoadImage(string key, PathLike? imagePath, Func<string, Bitmap> loaderFunction)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(typeof(ImageCache).FullName);
        }

        string pathString = imagePath?.PathString ?? string.Empty;
        if (pathString == string.Empty)
        {
            return Task.FromResult(defaultBitmap);
        }

        lock (Mutex)
        {
            if (cache.TryGetValue(key, out Bitmap? cachedThumbnail))
            {
                return Task.FromResult(cachedThumbnail!);
            }
        }

        return Task.Run(() =>
        {
            logger.Log($"Attempting to load bitmap for key [{key}].");
            Bitmap bitmap = loaderFunction.Invoke(pathString) ?? defaultBitmap;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(9))
                .RegisterPostEvictionCallback(OnImageEvicted);

            lock (Mutex)
            {
                if (cache.TryGetValue(key, out Bitmap? cachedThumbnail2))
                {
                    logger.Log($"Secondary cache lookup returned result for key [{key}]. Possible concurrency issue.");
                    bitmap.Dispose();
                    return cachedThumbnail2!;
                }

                knownCacheKeys.Add(key);
                cache.Set(key, bitmap, cacheEntryOptions);
            }

            return bitmap;
        });
    }

    private void OnImageEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is not Bitmap image)
        {
            return;
        }

        logger.Log($"Image [{key}] was evicted from cache.");
        try
        {
            image.Dispose();
        }
        catch (Exception e)
        {
            logger.Error($"Failed to properly dispose of image [{key}].", e);
        }
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        if (sender != timer)
        {
            return;
        }

        lock (Mutex)
        {
            foreach (string key in knownCacheKeys)
            {
                // Cache eviction is triggered upon attempting to get an item
                // out of cache that has been accessed within N minutes.
                cache.TryGetValue(key, out object? _);
            }
        }
    }
}