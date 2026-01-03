namespace ImageViewer.Config;

using ImageViewer.Models;

public sealed class Config
{
    public Config() { }

    public Config(Config source)
    {
        SlideshowIntervalMillis = source.SlideshowIntervalMillis;
        ScanDepth = source.ScanDepth;
        SortMethod = source.SortMethod;
    }

    public int SlideshowIntervalMillis { get; set; } = 5_000;

    public int ScanDepth { get; set; } = 1;

    public SortMethod SortMethod { get; set; } = SortMethod.WindowsLike;
}