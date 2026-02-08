namespace ImageViewer.Tests;

using System;
using ImageViewer.Models;

public static class TestData
{
    public static readonly PathLike CRoot = new(@"C:\");

    public static readonly string DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public static readonly PathLike DownloadPath = new PathLike(DesktopFolder).Parent().Join("Downloads");

    public static readonly PathLike ExecutingPath = new(AppContext.BaseDirectory);
    public static readonly PathLike ImageFolderPath = ExecutingPath.Join("TestImages");

    public static readonly string[] Images = [
        "TestImage1.png",
        "TestImage2.png",
        "TestImage3.png",
    ];
}