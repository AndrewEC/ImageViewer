namespace ImageViewer.Preview;

using System;
using Avalonia;
using ImageViewer.Models;

public sealed class CanvasImageManager
{

    private const double DefaultScale = 100;
    private const double MaxScale = DefaultScale * 5;
    private const double MinScale = DefaultScale / 5;
    private const int ScaleMultiplier = 10;

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

    public void IncrementScale(double amount)
    {
        ImageScale += amount * ScaleMultiplier;
    }

    public void Reset(bool keepScale)
    {
        if (!keepScale)
        {
            ImageScale = DefaultScale;
        }
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

        if (imageWidth > maxWidth)
        {
            double scaleDown = imageWidth / maxWidth;
            imageWidth = maxWidth;
            imageHeight /= scaleDown;
        }

        if (imageHeight > maxHeight)
        {
            double scaleDown = imageHeight / maxHeight;
            imageHeight = maxHeight;
            imageWidth /= scaleDown;
        }

        return current
            .WithWidth((int) (imageWidth * (ImageScale / 100.0)))
            .WithHeight((int) (imageHeight * (ImageScale / 100.0)));
    }
    
}