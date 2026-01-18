namespace ImageViewer.Core.ViewModels;

using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ImageViewer.Core.Config;
using ImageViewer.Core.Models;
using ImageViewer.Core.Utils;
using ImageViewer.Core.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

#pragma warning disable CA1001
public partial class ImagePreviewViewModel : ViewModelBase
#pragma warning restore CA1001
{
    private static readonly int[] NormalColumnSizes = [10, 80, 10];
    private static readonly int[] SlideshowColumnSizes = [0, 100, 0];

    private const double DefaultScale = 100;
    private const double MaxScale = DefaultScale * 2;
    private const double MinScale = DefaultScale / 2;

    private readonly ConsoleLogger<ImagePreviewViewModel> logger = new();

    private readonly Canvas canvas;
    private readonly Image referenceImage;
    private readonly Grid canvasGrid;
    private readonly DragManager canvasDragManager;

    private Timer? slideshowTimer;
    private double imageScale = DefaultScale;
    private double offsetX;
    private double offsetY;

    public ImagePreviewViewModel(ImagePreview parent)
    {
        Handlers.RegisterPropertyChangeHandler(AppState.Instance, new()
        {
            {
                nameof(AppState.Instance.SelectedImage),
                () => SelectedImage = AppState.Instance.SelectedImage
            },
            {
                nameof(AppState.Instance.SelectedFolderResources),
                () => IsNavigationEnabled = AppState.Instance.SelectedFolderResources.Count > 0
            },
            {
                nameof(AppState.Instance.IsSlideshowRunning),
                () => IsSlideshowRunning = AppState.Instance.IsSlideshowRunning
            },
        });

        NextImageCommand = ReactiveCommand.Create(NextImage);
        PreviousImageCommand = ReactiveCommand.Create(PreviousImage);
        StartSlideshowCommand = ReactiveCommand.Create(SetSlideshowTrue);
        StopSlideshowCommand = ReactiveCommand.Create(SetSlideshowFalse);
        DeleteImageCommand = ReactiveCommand.Create(DeleteImage);
        ShowInExplorerCommand = ReactiveCommand.Create(ShowInExplorer);

        Button nextButton = parent.FindControl<Button>("NextButton")!;
        Button previousButton = parent.FindControl<Button>("PreviousButton")!;
        Button stopSlideshowButton = parent.FindControl<Button>("StopSlideshowButton")!;
        canvasGrid = parent.FindControl<Grid>("CanvasGrid")!;

        canvas = parent.FindControl<Canvas>("ImageCanvas")!;
        canvas.SizeChanged += OnCanvasSizeChanged;
        canvas.PointerWheelChanged += OnCanvasScroll;

        referenceImage = parent.FindControl<Image>("ReferenceImage")!;
        referenceImage.SizeChanged += OnReferenceImageSizeChanged;

        canvasDragManager = new DragManager(canvas);
        canvasDragManager.ElementDragged += OnCanvasDragged;

        HotKeyManager.SetHotKey(nextButton, new KeyGesture(Key.D));
        HotKeyManager.SetHotKey(previousButton, new KeyGesture(Key.A));
        HotKeyManager.SetHotKey(stopSlideshowButton, new KeyGesture(Key.Escape));
    }

    public ReactiveCommand<Unit, Unit> NextImageCommand { get; }

    public ReactiveCommand<Unit, Unit> PreviousImageCommand { get; }

    public ReactiveCommand<Unit, Unit> StartSlideshowCommand { get; }

    public ReactiveCommand<Unit, Unit> StopSlideshowCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteImageCommand { get; }

    public ReactiveCommand<Unit, Unit> ShowInExplorerCommand { get; }

    private ImageRect imageRect = new(0, 0, 1, 1);
    public ImageRect ImageRect
    {
        get => imageRect;
        set => this.RaiseAndSetIfChanged(ref imageRect, value);
    }

    private bool isNavigationEnabled;
    public bool IsNavigationEnabled
    {
        get => isNavigationEnabled;
        set => this.RaiseAndSetIfChanged(ref isNavigationEnabled, value);
    }

    private bool isImageSelected;
    public bool IsImageSelected
    {
        get => isImageSelected;
        set => this.RaiseAndSetIfChanged(ref isImageSelected, value);
    }

    private bool isSlideshowRunning;
    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set
        {
            this.RaiseAndSetIfChanged(ref isSlideshowRunning, value);
            SetScale(DefaultScale);
            if (value)
            {
                StartSlideshow();
                GridUtil.ResizeColumns(canvasGrid, SlideshowColumnSizes);
            }
            else
            {
                StopSlideshow();
                GridUtil.ResizeColumns(canvasGrid, NormalColumnSizes);
            }
        }
    }

    private ImageResource? selectedImage;
    public ImageResource? SelectedImage
    {
        get => selectedImage;
        set
        {
            this.RaiseAndSetIfChanged(ref selectedImage, value);
            IsImageSelected = value != null;

            imageScale = 100;
            offsetX = 0;
            offsetY = 0;
            canvasDragManager.Reset();
            ResizeImage();
        }
    }

    private void OnCanvasScroll(object? sender, PointerWheelEventArgs e)
    {
        if (sender != canvas)
        {
            return;
        }

        SetScale(imageScale + e.Delta.Y * 5);
    }

    private void OnCanvasDragged(object? sender, InputElementDraggedEventArgs e)
    {
        if (sender != canvasDragManager)
        {
            return;
        }

        offsetX = e.Delta.X;
        offsetY = e.Delta.Y;

        ImageRect = ResizeImage();
    }

    private void SetScale(double nextScale)
    {
        imageScale = nextScale;

        if (imageScale > MaxScale)
        {
            imageScale = MaxScale;
            return;
        }
        else if (imageScale < MinScale)
        {
            imageScale = MinScale;
            return;
        }

        ImageRect = ResizeImage();
    }

    private void OnReferenceImageSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender != referenceImage)
        {
            return;
        }

        Size canvasSize = new(canvas.Bounds.Width, canvas.Bounds.Height);
        Size referenceImageSize = e.NewSize;

        ImageRect = ResizeImage(canvasSize, referenceImageSize);
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender != canvas)
        {
            return;
        }

        Size canvasSize = e.NewSize;
        Size referenceImageSize = new(referenceImage.Bounds.Width, referenceImage.Bounds.Height);

        ImageRect = ResizeImage(canvasSize, referenceImageSize);
    }

    private ImageRect ResizeImage()
    {
        Size canvasSize = new(canvas.Bounds.Width, canvas.Bounds.Height);
        Size referenceImageSize = new(referenceImage.Bounds.Width, referenceImage.Bounds.Height);
        return ResizeImage(canvasSize, referenceImageSize);
    }

    private ImageRect ResizeImage(Size canvasSize, Size referenceImageSize)
    {
        ImageRect nextDimensions = WithNewSize(ImageRect, canvasSize, referenceImageSize, imageScale);
        return WithNewPosition(nextDimensions, canvasSize);
    }

    private ImageRect WithNewPosition(ImageRect imageRect, Size canvasSize)
    {
        int x = (int) (canvasSize.Width / 2 - imageRect.Width / 2 + offsetX);
        int y = (int) (canvasSize.Height / 2 - imageRect.Height / 2 + offsetY);
        return imageRect.WithX(x).WithY(y);
    }

    private static ImageRect WithNewSize(ImageRect current, Size canvasSize, Size referenceImageSize, double scale)
    {
        double maxWidth = canvasSize.Width;
        double maxHeight = canvasSize.Height;
        
        double imageWidth = referenceImageSize.Width;
        double imageHeight = referenceImageSize.Height;

        if (imageWidth <= maxWidth && imageHeight <= maxHeight)
        {
            int width = (int) (imageWidth * (scale / 100.0));
            int height = (int) (imageHeight * (scale / 100.0));
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

        int newWidth = (int) (imageWidth * ratio * (scale / 100.0));
        int newHeight = (int) (imageHeight * ratio * (scale / 100.0));

        return current.WithWidth(newWidth)
            .WithHeight(newHeight);
    }

    private void PreviousImage()
    {
        if (selectedImage == null)
        {
            return;
        }

        List<ImageResource> resources = AppState.Instance.SelectedFolderResources;
        int index = resources.FindIndex(resource => resource.Path.Equals(selectedImage.Path));
        if (index == -1)
        {
            logger.Log("Could not move to next image because the currently selected image could not be found.");
            return;
        }

        if (index - 1 < 0)
        {
            index = resources.Count - 1;
        }
        else
        {
            index--;
        }

        AppState.Instance.SelectedImage = resources[index];
    }

    private void NextImage()
    {
        if (selectedImage == null)
        {
            return;
        }

        List<ImageResource> resources = AppState.Instance.SelectedFolderResources;
        int index = resources.FindIndex(resource => resource.Path.Equals(selectedImage.Path));
        if (index == -1)
        {
            logger.Log("Could not move to next image because the currently selected image could not be found.");
            return;
        }

        AppState.Instance.SelectedImage = resources[++index % resources.Count];
    }

    private void SetSlideshowTrue() => AppState.Instance.IsSlideshowRunning = true;

    private void SetSlideshowFalse() => AppState.Instance.IsSlideshowRunning = false;

    private void StartSlideshow()
    {
        logger.Log("Starting slideshow...");
        if (slideshowTimer != null)
        {
            return;
        }

        StopSlideshowTimer();

        int interval = ConfigState.Instance.LoadConfig().SlideshowIntervalMillis;
        logger.Log($"Starting slideshow timer with interval of: [{interval}]");
        slideshowTimer = new Timer(interval);
        slideshowTimer.Elapsed += (sender, e) => NextImage();
        slideshowTimer.AutoReset = true;
        slideshowTimer.Enabled = true;
    }

    private void StopSlideshow() => StopSlideshowTimer();

    private void StopSlideshowTimer()
    {
        if (slideshowTimer == null)
        {
            return;
        }

        logger.Log("Stopping slideshow timer.");
        slideshowTimer.Stop();
        slideshowTimer.Dispose();
        slideshowTimer = null;
    }

    private async void DeleteImage()
    {
        if (SelectedImage == null)
        {
            return;
        }

        logger.Log($"User requested deletion of currently selected image: [{SelectedImage.Path.PathString}]");

        var result = await MessageBoxManager.GetMessageBoxStandard(
            "Delete Image",
            "Are you sure you want to delete this image? This action cannot be undone.",
            ButtonEnum.YesNo)
            .ShowAsync();

        if (result != ButtonResult.Yes)
        {
            return;
        }

        if (SelectedImage.Path.Delete())
        {
            AppState.Instance.RemoveCurrentlySelectedImageFromFolderResources();
        }
    }

    private void ShowInExplorer()
    {
        if (SelectedImage == null)
        {
            return;
        }

        Process.Start("explorer.exe", "/select," + SelectedImage.Path.PathString);
    }
}