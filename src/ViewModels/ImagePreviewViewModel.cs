namespace ImageViewer.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using Avalonia.Threading;
using ImageViewer.Log;
using ImageViewer.Models;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

/// <summary>
/// The view model for <see cref="ImagePreviewView"/>.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Reviewed.")]
public class ImagePreviewViewModel : ReactiveObject
{
    private readonly ConsoleLogger<ImagePreviewViewModel> logger = new();
    private readonly AppStateProperties appState;
    private DispatcherTimer? slideshowTimer;

    /// <summary>
    /// Initializes the image preview view model.
    /// </summary>
    /// <param name="appState">The root application state. This view model will listen
    /// for changes on the <see cref="AppStateProperties.Images"/> property.</param>
    public ImagePreviewViewModel(AppStateProperties appState)
    {
        this.appState = appState;
        appState.PropertyChanged += OnAppStateChanged;

        ViewNextImageCommand = ReactiveCommand.Create(ViewNextImage);
        ViewPreviousImageCommand = ReactiveCommand.Create(ViewPreviousImage);
    }

    /// <summary>
    /// Gets the command to be invoked when the user clicks on the "Next" button to
    /// view the next available image.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewNextImageCommand { get; }

    /// <summary>
    /// Gets the command to be invoked when the user clicks on the "Previous" button
    /// to view the previous available image.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewPreviousImageCommand { get; }

    private bool isImageSelected = false;

    /// <summary>
    /// Gets or sets a value indicating whether the user has selected an image that
    /// can be previewed.
    /// </summary>
    public bool IsImageSelected
    {
        get => isImageSelected;
        set => this.RaiseAndSetIfChanged(ref isImageSelected, value);
    }

    private ImageItem? selectedImage = null;

    /// <summary>
    /// Gets or sets the image the user has currently selected.
    /// </summary>
    public ImageItem? SelectedImage
    {
        get => selectedImage;
        set => this.RaiseAndSetIfChanged(ref selectedImage, value);
    }

    private bool isSlideshowRunning = false;

    /// <summary>
    /// Gets or sets a value indicating whether the slidshow function is
    /// currently running.
    /// </summary>
    public bool IsSlideshowRunning
    {
        get => !isSlideshowRunning;
        set => this.RaiseAndSetIfChanged(ref isSlideshowRunning, value);
    }

    /// <summary>
    /// Starts the slideshow. A slideshow cannot be started if one is already running
    /// or if the user has not yet selected an image to preview.
    /// </summary>
    public async void StartSlideshow()
    {
        logger.Log("Starting slideshow.");
        if (!IsSlideshowRunning || SelectedImage == null)
        {
            logger.Log("Slideshow will not start because it is already running or there is no image selected.");
            return;
        }

        await MessageBoxManager.GetMessageBoxStandard(
            "Slideshow.",
            "Use the space key to stop the slideshow at any time.",
            ButtonEnum.Ok).ShowAsync();

        slideshowTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(2000),
            DispatcherPriority.Normal,
            OnSlideshowTimerTick);

        IsSlideshowRunning = true;
    }

    /// <summary>
    /// Stops the running slideshow. This has no effect if the slideshow
    /// has not yet been started.
    /// </summary>
    public void StopSlideshow()
    {
        logger.Log("Stopping slideshow.");
        if (slideshowTimer == null)
        {
            logger.Log("Slideshow cannot be stopped because it has not yet been started.");
            return;
        }

        slideshowTimer.Stop();
        IsSlideshowRunning = false;
    }

    /// <summary>
    /// Navigates to the next available image to view. This will wrap around
    /// to the first available image if the user is currently on the last image
    /// when this is invoked.
    /// </summary>
    public void ViewNextImage()
    {
        logger.Log("Navigating to next image.");
        if (!ChangeActiveImage((index) => index + 1))
        {
            ViewFirstImage();
        }
    }

    /// <summary>
    /// Navigates to the previous available image to view. This will wrap around
    /// to the last available image if the user is currently on the first image
    /// when this is invoked.
    /// </summary>
    public void ViewPreviousImage()
    {
        logger.Log("Navigating to previous image.");
        if (!ChangeActiveImage((index) => index - 1))
        {
            ViewLastImage();
        }
    }

    private void OnSlideshowTimerTick(object? sender, EventArgs e)
    {
        ViewNextImage();
    }

    private void ViewFirstImage()
    {
        logger.Log("Navigating to first image.");
        appState.SelectedFolder = appState.Folders[0];
        appState.SelectedImage = appState.Images[0];
    }

    private void ViewLastImage()
    {
        logger.Log("Navigating to last image.");
        appState.SelectedFolder = appState.Folders[^1];
        appState.SelectedImage = appState.Images[^1];
    }

    private bool ChangeActiveImage(Func<int, int> indexMapper)
    {
        logger.Log("Changing selected image.");
        if (SelectedImage == null)
        {
            logger.Log("Could not change selected image because no image is currently selected.");
            return false;
        }

        int index = Array.IndexOf(appState.Images, SelectedImage);
        if (!appState.Images.IsValidIndex(index))
        {
            logger.Log($"Could not find index of current image. Index lookup return [{index}].");
            return false;
        }

        int nextIndex = indexMapper.Invoke(index);
        if (appState.Images.IsValidIndex(nextIndex))
        {
            appState.SelectedImage = appState.Images[nextIndex];
            return true;
        }

        logger.Log($"Next index [{nextIndex}] was outside bounds of image array. Switching active folder.");

        if (!ChangeActiveFolder(indexMapper))
        {
            return false;
        }

        if (nextIndex < index)
        {
            logger.Log("Switching to last image in new folder.");
            appState.SelectedImage = appState.Images[^1];
        }
        else
        {
            logger.Log("Switching to first image in new folder.");
            appState.SelectedImage = appState.Images[0];
        }

        return true;
    }

    private bool ChangeActiveFolder(Func<int, int> indexMapper)
    {
        logger.Log("Changing selected folder.");
        if (appState.SelectedFolder == null)
        {
            logger.Log("Could not change selected folder because no folder is currently selected.");
            return false;
        }

        int index = Array.IndexOf(appState.Folders, appState.SelectedFolder);
        if (!appState.Folders.IsValidIndex(index))
        {
            logger.Log($"Could not find index of current folder. Index lookup return [{index}].");
            return false;
        }

        int nextIndex = indexMapper.Invoke(index);
        if (!appState.Folders.IsValidIndex(nextIndex))
        {
            logger.Log($"Next folder index of [{nextIndex}] was outside folder array bounds. Folder will not be changed.");
            return false;
        }

        appState.SelectedFolder = appState.Folders[nextIndex];
        return true;
    }

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != appState)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(appState.IsImageSelected):
                IsImageSelected = appState.IsImageSelected;
                break;
            case nameof(appState.SelectedImage):
                SelectedImage = appState.SelectedImage;
                break;
        }
    }
}
