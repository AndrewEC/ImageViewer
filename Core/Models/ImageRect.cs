namespace ImageViewer.Core.ViewModels;

public readonly struct ImageRect
{
    public ImageRect() { }

    public ImageRect(int width, int height, int x, int y)
    {
        Width = width;
        Height = height;
        X = x;
        Y = y;
    }
    
    public int Width { get; }

    public int Height { get; }

    public int X { get; }

    public int Y { get; }

    public ImageRect WithWidth(int width) => new(width, Height, X, Y);

    public ImageRect WithHeight(int height) => new(Width, height, X, Y);

    public ImageRect WithX(int x) => new(Width, Height, x, Y);

    public ImageRect WithY(int y) => new(Width, Height, X, y);

}