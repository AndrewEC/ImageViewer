namespace ImageViewer.Preview;

using System;
using Avalonia;
using ImageViewer.ViewModels;

public sealed class CanvasImageManager
{

    private const double DefaultScale = 100;
    private const double MaxScale = DefaultScale * 2;
    private const double MinScale = DefaultScale / 2;

    public event EventHandler<ImageRectChangedEventArgs>? ImageRectChanged;

    public Point Offset
    {
        get;

        set
        {
            field = value;
            ComputeImageRect();
        }
    }

    public Size CanvasSize
    {
        get;

        set
        {
            field = value;
            ComputeImageRect();
        }
    }

    public Size ImageSize
    {
        get;

        set
        {
            field = value;
            ComputeImageRect();
        }
    }

    public double ImageScale
    {
        get;

        set
        {
            field = value;
            OnScaleUpdated();
        }

    } = DefaultScale;

    public ImageRect ImageRect
    {
        get;

        private set
        {
            field = value;
            ImageRectChanged?.Invoke(this, new ImageRectChangedEventArgs(value));
        }
    }

    public void Reset()
    {
        ImageScale = DefaultScale;
        Offset = new Point();
        ComputeImageRect();
    }

    private void OnScaleUpdated()
    {
        if (ImageScale > MaxScale)
        {
            ImageScale = MaxScale;
            return;
        }
        else if (ImageScale < MinScale)
        {
            ImageScale = MinScale;
            return;
        }

        ComputeImageRect();
    }

    private void ComputeImageRect()
    {
        ImageRect nextDimensions = WithNewSize(ImageRect);
        ImageRect = WithNewPosition(nextDimensions);
    }

    private ImageRect WithNewPosition(ImageRect imageRect)
    {
        int x = (int) (CanvasSize.Width / 2 - imageRect.Width / 2 + Offset.X);
        int y = (int) (CanvasSize.Height / 2 - imageRect.Height / 2 + Offset.Y);
        return imageRect.WithX(x).WithY(y);
    }

    private ImageRect WithNewSize(ImageRect current)
    {
        double maxWidth = CanvasSize.Width;
        double maxHeight = CanvasSize.Height;
        
        double imageWidth = ImageSize.Width;
        double imageHeight = ImageSize.Height;

        if (imageWidth <= maxWidth && imageHeight <= maxHeight)
        {
            int width = (int) (imageWidth * (ImageScale / 100.0));
            int height = (int) (imageHeight * (ImageScale / 100.0));
            return current.WithWidth(width).WithHeight(height);
        }

        double ratio;
        if (imageWidth - maxWidth > imageHeight - maxHeight)
        {
            ratio = maxWidth / imageWidth;
        }
        else
        {
            ratio = maxHeight / imageHeight;
        }

        int newWidth = (int) (imageWidth * ratio * (ImageScale / 100.0));
        int newHeight = (int) (imageHeight * ratio * (ImageScale / 100.0));

        return current.WithWidth(newWidth).WithHeight(newHeight);
    }
    
}