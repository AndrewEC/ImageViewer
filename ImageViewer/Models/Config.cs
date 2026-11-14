namespace ImageViewer.Models;

using System;

public struct Config
{
    public Config() { }

    public Config(Config source)
        : this(source.SlideshowIntervalMillis, source.ScanDepth, source.SortMethod, source.RandomizeSlideshow, source.MaintainZoom) { }

    public Config(int slideshowIntervalMillis, int scanDepth, SortMethod sortMethod, bool randomizeSlideshow, bool maintainZoom)
    {
        SlideshowIntervalMillis = slideshowIntervalMillis;
        ScanDepth = scanDepth;
        SortMethod = sortMethod;
        RandomizeSlideshow = randomizeSlideshow;
        MaintainZoom = maintainZoom;
    }

    public int SlideshowIntervalMillis { get; set; } = 5_000;

    public int ScanDepth { get; set; } = 1;

    public SortMethod SortMethod { get; set; } = SortMethod.WindowsLike;

    public bool RandomizeSlideshow { get; set; }

    public bool MaintainZoom { get; set; }

    public override bool Equals(object? obj) => obj is Config other
        && other.SlideshowIntervalMillis == SlideshowIntervalMillis
        && other.ScanDepth == ScanDepth
        && other.SortMethod == SortMethod
        && other.RandomizeSlideshow == RandomizeSlideshow
        && other.MaintainZoom == MaintainZoom;

    public override readonly int GetHashCode()
        => HashCode.Combine(SlideshowIntervalMillis, ScanDepth, SortMethod, RandomizeSlideshow, MaintainZoom);

    public static bool operator ==(Config left, Config right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Config left, Config right)
    {
        return !(left == right);
    }
}