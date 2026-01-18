namespace ImageViewer.Core.Preview;

using System;
using ImageViewer.Core.ViewModels;

public sealed class ImageRectChangedEventArgs(ImageRect rect) : EventArgs
{
    public ImageRect Rect { get; } = rect;
}