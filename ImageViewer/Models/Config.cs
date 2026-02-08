namespace ImageViewer.Models;

using System;

public sealed class Config
{
    public Config() { }

    public Config(Config source)
        : this(source.SlideshowIntervalMillis, source.ScanDepth, source.SortMethod) { }

    public Config(int slideshowIntervalMillis, int scanDepth, SortMethod sortMethod)
    {
        SlideshowIntervalMillis = slideshowIntervalMillis;
        ScanDepth = scanDepth;
        SortMethod = sortMethod;
    }

    public int SlideshowIntervalMillis { get; set; } = 5_000;

    public int ScanDepth { get; set; } = 1;

    public SortMethod SortMethod { get; set; } = SortMethod.WindowsLike;

    public override bool Equals(object? obj) => obj is Config other
        && other.SlideshowIntervalMillis == SlideshowIntervalMillis
        && other.ScanDepth == ScanDepth
        && other.SortMethod == SortMethod;

    public override int GetHashCode()
        => HashCode.Combine(SlideshowIntervalMillis, ScanDepth, SortMethod);
}