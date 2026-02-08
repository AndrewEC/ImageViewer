namespace ImageViewer.Preview;

using System;
using Avalonia;

public sealed class InputElementDraggedEventArgs(Point delta) : EventArgs
{
    public Point Delta { get; } = delta;
}