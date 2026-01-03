namespace ImageViewer.State;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Media.Imaging;
using ImageViewer.Utils;

public sealed class ImageCache
{
    private static readonly object SyncLock = new();
    private static readonly string ImageKeyPrefix = "image-";
    private static readonly string ThumbnailKeyPrefix = "thumnail-";
    private static readonly int ThumbnailHeight = 300;
    private static readonly int TimerTickInterval = 30_000;
    private static readonly int CacheEntryTimeToLiveMillis = 1000 * 60 * 10;

    public static readonly ImageCache Instance = new();

    private readonly ConsoleLogger<ImageCache> logger = new();

    private readonly Dictionary<string, CacheEntry> cache = [];

    private ImageCache()
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
            foreach (KeyValuePair<string, CacheEntry> entry in cache)
            {
                if (HasExpired(entry.Value.Timestamp))
                {
                    logger.Log($"Disposing of expired image: [{entry.Key}].");
                    cache.Remove(entry.Key);
                    try
                    {
                        entry.Value.Bitmap.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Failed to dispose bitmap [{entry.Key}]. Cause: [{ex.Message}]");
                    }
                }
            }
        }
    }

    private static bool HasExpired(long entryTimestamp)
        => CurrentTimestamp() - entryTimestamp > CacheEntryTimeToLiveMillis;

    private static long CurrentTimestamp() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public Task<Bitmap?> LoadImage(string path)
        => Task.Run(() => DoLoadImage(path, false));

    public Task<Bitmap?> LoadThumbnail(string path)
        => Task.Run(() => DoLoadImage(path, true));

    private Bitmap? DoLoadImage(string path, bool thumbnail)
    {
        string keyPrefix = thumbnail ? ThumbnailKeyPrefix : ImageKeyPrefix;
        string cacheKey = keyPrefix + path;

        logger.Log($"Loading image [{path}] with key [{cacheKey}]");

        lock (SyncLock)
        {
            if (cache.TryGetValue(cacheKey, out CacheEntry? entry))
            {
                entry.Timestamp = CurrentTimestamp();
                logger.Log($"Found cached value for key [{cacheKey}]. Updating timestamp to [{entry.Timestamp}]");
                return (Bitmap?)entry.Bitmap;
            }
        }

        Bitmap? newImage = null;
        try
        {
            using (FileStream stream = File.OpenRead(path))
            {
                if (thumbnail)
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
            logger.Log($"Failed to load image [{path}]. Cause: [{e.Message}]");
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
                return newImage;
            }
        }
    }

    private sealed class CacheEntry(Bitmap bitmap, long timestamp)
    {
        public Bitmap Bitmap { get; } = bitmap;

        public long Timestamp { get; set; } = timestamp;
    }
}