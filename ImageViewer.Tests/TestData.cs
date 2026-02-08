namespace ImageViewer.Tests;

using System;
using ImageViewer.Models;

public static class TestData
{
    public readonly record struct ImageData(string FileName, int Width, int Height);

    public static readonly string DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public static readonly PathLike DownloadPath = new PathLike(DesktopFolder).Parent().Join("Downloads");

    public static readonly PathLike ExecutingPath = new(AppContext.BaseDirectory);
    public static readonly PathLike TestImagePath = ExecutingPath.Join("TestImages");

    public static readonly string[] TestImages = [
        "TestImage1.png",
        "TestImage2.png",
        "TestImage3.png",
    ];
}