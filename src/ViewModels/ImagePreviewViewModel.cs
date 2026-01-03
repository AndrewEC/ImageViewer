namespace ImageViewer.ViewModels;

using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Input;
using ImageViewer.Config;
using ImageViewer.Models;
using ImageViewer.Utils;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

#pragma warning disable CA1001
public partial class ImagePreviewViewModel : ViewModelBase
#pragma warning restore CA1001
{
    private readonly ConsoleLogger<ImagePreviewViewModel> logger = new();

    private Timer? slideshowTimer;

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
            if (value)
            {
                StartSlideshow();
            }
            else
            {
                StopSlideshow();
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
        }
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