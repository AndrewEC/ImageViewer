namespace ImageViewer.State;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Media.Imaging;
using ImageViewer.Utils;

public interface IImageCache
{
    Task<Bitmap?> LoadImage(string path);

    Task<Bitmap?> LoadThumbnail(string path);
}

public sealed class ImageCache : IImageCache
{
    private static readonly System.Threading.Lock SyncLock = new();
    private static readonly string ImageKeyPrefix = "image-";
    private static readonly string ThumbnailKeyPrefix = "thumnail-";
    private static readonly int ThumbnailHeight = 300;
    private static readonly int TimerTickInterval = 1000 * 60; // 1 minute in milliseconds.
    private static readonly int CacheEntryTimeToLiveMillis = 1000 * 60 * 10; // 10 minutes in milliseconds.
    private static readonly int MaxCacheSize = 30;

    private readonly ConsoleLogger<ImageCache> logger = new();

    private readonly Dictionary<string, CacheEntry> cache = [];

    public ImageCache()
    {
        Timer timer = new(TimerTickInterval);
        timer.Elapsed += OnTimerTick;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        lock (SyncLock)
        {
            logger.Log("Timer ticked. Checking for expired images...");

            long currentTime = CurrentTimestamp();
            foreach (KeyValuePair<string, CacheEntry> entry in cache)
            {
                if (currentTime - entry.Value.Timestamp > CacheEntryTimeToLiveMillis)
                {
                    logger.Log($"Disposing of expired image: [{entry.Key}].");
                    cache.Remove(entry.Key);
                    try
                    {
                        entry.Value.Bitmap.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to dispose bitmap [{entry.Key}].", ex);
                    }
                }
            }
        }
    }

    private static long CurrentTimestamp() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public Task<Bitmap?> LoadImage(string path)
        => Task.Run(() => DoLoadImage(path, ImageKeyPrefix));

    public Task<Bitmap?> LoadThumbnail(string path)
        => Task.Run(() => DoLoadImage(path, ThumbnailKeyPrefix));

    private Bitmap? DoLoadImage(string path, string keyPrefix)
    {
        string cacheKey = keyPrefix + path;

        logger.Log($"Loading image [{path}] with key [{cacheKey}]");

        lock (SyncLock)
        {
            if (cache.TryGetValue(cacheKey, out CacheEntry? entry))
            {
                long currentTime = CurrentTimestamp();
                logger.Log($"Found cached value for key [{cacheKey}]. Updating timestamp to [{currentTime}]");
                cache[cacheKey] = entry.WithTimestamp(CurrentTimestamp());
                return (Bitmap?)entry.Bitmap;
            }
        }

        Bitmap? newImage = null;
        try
        {
            using (FileStream stream = File.OpenRead(path))
            {
                if (keyPrefix == ThumbnailKeyPrefix)
                {
                    newImage = Bitmap.DecodeToHeight(stream, ThumbnailHeight, BitmapInterpolationMode.LowQuality);
                }
                else
                {
                    newImage = new Bitmap(stream);
                }
            }
        }
        catch (Exception e)
        {
            logger.Error($"Failed to load image [{path}].", e);
            return null;
        }

        lock (SyncLock)
        {
            if (cache.TryGetValue(cacheKey, out CacheEntry? entry))
            {
                return (Bitmap?)entry.Bitmap;
            }
            else
            {
                cache[cacheKey] = new CacheEntry(newImage, CurrentTimestamp());
                TryPopOldestImageEntry();
                return newImage;
            }
        }
    }

    private void TryPopOldestImageEntry()
    {
        string[] imageKeys = [.. cache.Keys.Where(key => key.StartsWith(ImageKeyPrefix, StringComparison.InvariantCulture))];
        if (imageKeys.Length < MaxCacheSize)
        {
            return;
        }

        logger.Log("Popping oldest item from cache.");

        string oldestKey = imageKeys[0];
        CacheEntry oldestEntry = cache[oldestKey];
        foreach (string nextKey in imageKeys)
        {
            if (nextKey == oldestKey)
            {
                continue;
            }

            CacheEntry nextEntry = cache[nextKey];
            if (nextEntry.Timestamp < oldestEntry.Timestamp)
            {
                oldestKey = nextKey;
                oldestEntry = nextEntry;
            }
        }

        logger.Log($"Found oldest image item with key: [{oldestKey}].");
        cache.Remove(oldestKey);

        try
        {
            oldestEntry.Bitmap.Dispose();
        }
        catch (Exception e)
        {
            logger.Error("Could not dispose of oldest cached image.", e);
        }
    }

    private sealed record class CacheEntry(Bitmap Bitmap, long Timestamp)
    {
        public CacheEntry WithTimestamp(long timestamp) => new(Bitmap, timestamp);
    }
}