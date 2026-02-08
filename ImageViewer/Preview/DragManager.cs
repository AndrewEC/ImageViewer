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

        this.targetElement.PointerPressed += OnCanvasPointerPressed;
        this.targetElement.PointerReleased += OnCavasPointerReleased;
        this.targetElement.PointerMoved += OnCanvasPointerMoved;
    }

    public void Reset()
    {
        dragging = false;
        start = new Point();
        lastDelta = new Point();
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender != targetElement || !dragging)
        {
            return;
        }

        Point position = e.GetCurrentPoint(null).Position;

        Point delta = GetDelta(position);

        ElementDragged?.Invoke(this, new InputElementDraggedEventArgs(delta));
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender != targetElement || !e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        dragging = true;

        start = e.GetCurrentPoint(null).Position;
    }

    private void OnCavasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender != targetElement)
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