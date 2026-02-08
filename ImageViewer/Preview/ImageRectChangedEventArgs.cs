namespace ImageViewer.Preview;

using System;
using ImageViewer.ViewModels;

public sealed class ImageRectChangedEventArgs(ImageRect rect) : EventArgs
{
    public ImageRect Rect { get; } = rect;
}