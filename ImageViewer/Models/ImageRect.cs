namespace ImageViewer.Models;

public readonly record struct ImageRect(int Width, int Height, int X, int Y)
{
    public ImageRect() : this(0, 0, 0, 0) { }

    public ImageRect WithWidth(int width) => new(width, Height, X, Y);

    public ImageRect WithHeight(int height) => new(Width, height, X, Y);

    public ImageRect WithX(int x) => new(Width, Height, x, Y);

    public ImageRect WithY(int y) => new(Width, Height, X, y);

}