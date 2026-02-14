namespace ImageViewer.Preview;

using System;
using ImageViewer.Models;

public sealed class ImageRectChangedEventArgs(ImageRect rect) : EventArgs
{
    public ImageRect Rect { get; } = rect;
}