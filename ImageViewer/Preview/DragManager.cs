namespace ImageViewer.Preview;

using System;
using Avalonia;
using Avalonia.Input;

public sealed class DragManager
{

    public event EventHandler<InputElementDraggedEventArgs>? ElementDragged;
    
    private readonly InputElement targetElement;

    private bool dragging;
    private Point start;
    private Point lastDelta;

    public DragManager(InputElement targetElement)
    {
        this.targetElement = targetElement;

        this.targetElement.PointerPressed += OnElementPointerPressed;
        this.targetElement.PointerReleased += OnElementPointerReleased;
        this.targetElement.PointerMoved += OnElementPointerMoved;
    }

    public void Reset()
    {
        dragging = false;
        start = new Point();
        lastDelta = new Point();
    }

    private void OnElementPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender != targetElement || !dragging)
        {
            return;
        }

        Point position = e.GetCurrentPoint(null).Position;

        Point delta = GetDelta(position);

        ElementDragged?.Invoke(this, new InputElementDraggedEventArgs(delta));
    }

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender != targetElement || !e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        dragging = true;

        start = e.GetCurrentPoint(null).Position;
    }

    private void OnElementPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender != targetElement || !dragging)
        {
            return;
        }

        dragging = false;

        Point position = e.GetCurrentPoint(null).Position;

        lastDelta = GetDelta(position);

        start = new Point();
    }

    private Point GetDelta(Point current)
    {
        double xDelta = current.X - start.X + lastDelta.X;
        double yDelta = current.Y - start.Y + lastDelta.Y;
        return new(xDelta, yDelta);
    }

}