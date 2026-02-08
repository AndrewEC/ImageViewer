namespace ImageViewer.ViewModels;

using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Input;
using ImageViewer.Preview;
using ImageViewer.Models;
using ImageViewer.Utils;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System.Collections.Immutable;
using ImageViewer.Injection;
using ImageViewer.State;
using Avalonia.Platform.Storage;

#pragma warning disable CA1001
public partial class ImagePreviewViewModel : ViewModelBase
#pragma warning restore CA1001
{
    private static readonly ImmutableArray<int> NormalColumnSizes = [10, 80, 10];
    private static readonly ImmutableArray<int> SlideshowColumnSizes = [0, 100, 0];

    private readonly ConsoleLogger<ImagePreviewViewModel> logger = new();

    private readonly IAppState appState;
    private readonly IConfigState configState;
    private readonly IScan scan;
    private readonly IInitializer initializer;
    private readonly Canvas canvas;
    private readonly Image referenceImage;
    private readonly Grid canvasGrid;
    private readonly DragManager canvasDragManager;
    private readonly CanvasImageManager canvasImageManager;

    private Timer? slideshowTimer;

    public ImagePreviewViewModel(ImagePreview parent)
    {
        appState = ServiceContainer.GetService<IAppState>();
        configState = ServiceContainer.GetService<IConfigState>();
        scan = ServiceContainer.GetService<IScan>();
        initializer = ServiceContainer.GetService<IInitializer>();

        Handlers.RegisterPropertyChangeHandler(appState, new()
        {
            {
                nameof(appState.SelectedImage),
                () => SelectedImage = appState.SelectedImage
            },
            {
                nameof(appState.SelectedFolderResources),
                () => IsNavigationEnabled = appState.SelectedFolderResources.Count > 0
            },
            {
                nameof(appState.IsSlideshowRunning),
                () => IsSlideshowRunning = appState.IsSlideshowRunning
            },
        });

        NextImageCommand = ReactiveCommand.Create(NextImage);
        PreviousImageCommand = ReactiveCommand.Create(PreviousImage);
        StartSlideshowCommand = ReactiveCommand.Create(SetSlideshowTrue);
        StopSlideshowCommand = ReactiveCommand.Create(SetSlideshowFalse);
        DeleteImageCommand = ReactiveCommand.Create(DeleteImage);
        ShowInExplorerCommand = ReactiveCommand.Create(ShowInExplorer);

        canvasGrid = parent.FindControl<Grid>("CanvasGrid")!;
        canvasGrid.AddHandler(DragDrop.DragOverEvent, OnDragOverCanvas);
        canvasGrid.AddHandler(DragDrop.DropEvent, OnDropOnCanvas);

        canvas = parent.FindControl<Canvas>("ImageCanvas")!;
        canvas.SizeChanged += OnCanvasSizeChanged;
        canvas.PointerWheelChanged += OnCanvasScroll;

        referenceImage = parent.FindControl<Image>("ReferenceImage")!;
        referenceImage.SizeChanged += OnReferenceImageSizeChanged;

        canvasDragManager = new DragManager(canvas);
        canvasDragManager.ElementDragged += OnCanvasDragged;

        canvasImageManager = new CanvasImageManager();
        canvasImageManager.ImageRectChanged += OnImageRectChanged;

        Button nextButton = parent.FindControl<Button>("NextButton")!;
        Button previousButton = parent.FindControl<Button>("PreviousButton")!;
        Button stopSlideshowButton = parent.FindControl<Button>("StopSlideshowButton")!;
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

    public ImageRect ImageRect
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = new(0, 0, 1, 1);

    public bool IsNavigationEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsImageSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsSlideshowRunning
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            canvasImageManager.Reset();
            canvasDragManager.Reset();
            if (value)
            {
                StartSlideshow();
                GridUtil.ResizeColumns(canvasGrid, SlideshowColumnSizes);
            }
            else
            {
                StopSlideshowTimer();
                GridUtil.ResizeColumns(canvasGrid, NormalColumnSizes);
            }
        }
    }

    public ImageResource? SelectedImage
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            IsImageSelected = value != null;

            canvasDragManager.Reset();
            canvasImageManager.Reset();
        }
    }

    private void OnImageRectChanged(object? sender, ImageRectChangedEventArgs e)
    {
        if (sender != canvasImageManager)
        {
            return;
        }

        ImageRect = e.Rect;
    }

    private void OnCanvasScroll(object? sender, PointerWheelEventArgs e)
    {
        if (sender != canvas || IsSlideshowRunning)
        {
            return;
        }

        canvasImageManager.ImageScale += e.Delta.Y * 5;
    }

    private void OnCanvasDragged(object? sender, InputElementDraggedEventArgs e)
    {
        if (sender != canvasDragManager || IsSlideshowRunning)
        {
            return;
        }

        canvasImageManager.Offset = e.Delta;
    }

    private void OnReferenceImageSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender != referenceImage)
        {
            return;
        }

        canvasImageManager.ImageSize = e.NewSize;
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender != canvas)
        {
            return;
        }

        canvasImageManager.CanvasSize = e.NewSize;
    }

    private void OnDropOnCanvas(object? sender, DragEventArgs e)
    {
        IStorageItem[] files = e.DataTransfer.TryGetFiles() ?? [];
        if (files.Length != 1)
        {
            return;
        }

        PathLike filePath = new(files[0].Path.AbsolutePath);
        if (!scan.IsPotentiallyImageFile(filePath) || filePath.IsDirectory())
        {
            return;
        }

        initializer.InitializeAppFromPath(filePath);
    }

    private void OnDragOverCanvas(object? sender, DragEventArgs e)
    {
        IStorageItem[] files = e.DataTransfer.TryGetFiles() ?? [];
        if (files.Length != 1)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        PathLike filePath = new(files[0].Path.AbsolutePath);
        if (!scan.IsPotentiallyImageFile(filePath) || filePath.IsDirectory())
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
    }

    private void PreviousImage()
    {
        if (SelectedImage == null)
        {
            return;
        }

        List<ImageResource> resources = appState.SelectedFolderResources;
        int index = resources.FindIndex(resource => resource.Path.Equals(SelectedImage.Path));
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

        appState.SelectedImage = resources[index];
    }

    private void NextImage()
    {
        if (SelectedImage == null)
        {
            return;
        }

        List<ImageResource> resources = appState.SelectedFolderResources;
        int index = resources.FindIndex(resource => resource.Path.Equals(SelectedImage.Path));
        if (index == -1)
        {
            logger.Log("Could not move to next image because the currently selected image could not be found.");
            return;
        }

        appState.SelectedImage = resources[++index % resources.Count];
    }

    private void SetSlideshowTrue() => appState.IsSlideshowRunning = true;

    private void SetSlideshowFalse() => appState.IsSlideshowRunning = false;

    private void StartSlideshow()
    {
        logger.Log("Starting slideshow...");
        if (slideshowTimer != null)
        {
            return;
        }

        StopSlideshowTimer();

        int interval = configState.LoadConfig().SlideshowIntervalMillis;
        logger.Log($"Starting slideshow timer with interval of: [{interval}]");
        slideshowTimer = new Timer(interval);
        slideshowTimer.Elapsed += (sender, e) => NextImage();
        slideshowTimer.AutoReset = true;
        slideshowTimer.Enabled = true;
    }

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
            appState.RemoveCurrentlySelectedImageFromFolderResources();
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