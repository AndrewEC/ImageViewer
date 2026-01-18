namespace ImageViewer.Core.Events;

using System;
using Avalonia;
using ImageViewer.Core.ViewModels;

public sealed class CanvasImageSizeManager
{

    public const double DefaultScale = 100;
    private const double MaxScale = DefaultScale * 2;
    private const double MinScale = DefaultScale / 2;

    public event EventHandler<ImageRectChangedEventArgs>? ImageRectChanged;

    private Point offset;
    public Point Offset
    {
        get => offset;

        set
        {
            offset = value;
            ComputeImageRect();
        }
    }

    private Size canvasSize;
    public Size CanvasSize
    {
        get => canvasSize;

        set
        {
            canvasSize = value;
            ComputeImageRect();
        }
    }

    private Size imageSize;
    public Size ImageSize
    {
        get => imageSize;

        set
        {
            imageSize = value;
            ComputeImageRect();
        }
    }

    private double imageScale = DefaultScale;
    public double ImageScale
    {
        get => imageScale;

        set
        {
            imageScale = value;
            OnScaleUpdated();
        }

    }

    private ImageRect imageRect;
    public ImageRect ImageRect
    {
        get => imageRect;

        private set
        {
            imageRect = value;
            ImageRectChanged?.Invoke(this, new ImageRectChangedEventArgs(imageRect));
        }
    }

    public void Reset()
    {
        imageScale = DefaultScale;
        offset = new Point();
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
        int x = (int) (CanvasSize.Width / 2 - imageRect.Width / 2 + offset.X);
        int y = (int) (CanvasSize.Height / 2 - imageRect.Height / 2 + offset.Y);
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
        if (imageWidth > imageHeight)
        {
            ratio = maxWidth / imageWidth;
        }
        else
        {
            ratio = maxHeight / imageHeight;
        }

        int newWidth = (int) (imageWidth * ratio * (ImageScale / 100.0));
        int newHeight = (int) (imageHeight * ratio * (ImageScale / 100.0));

        return current.WithWidth(newWidth)
            .WithHeight(newHeight);
    }
    
}