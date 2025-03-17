namespace ImageViewer.ViewModels;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using Avalonia.Threading;
using ImageViewer.Log;
using ImageViewer.Models;
using ImageViewer.Util;
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
        appState.PropertyChanged += HelperExtensions.CreatePropertyChangeConsumer(
            appState,
            new()
            {
                { nameof(appState.SelectedImage), () => SelectedImage = appState.SelectedImage },
                { nameof(appState.IsSlideshowRunning), () => IsSlideshowRunning = appState.IsSlideshowRunning },
            });

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
        set
        {
            this.RaiseAndSetIfChanged(ref selectedImage, value);
            IsImageSelected = value != null;
            ImagePathExists = value?.Path.IsFile() ?? false;
        }
    }

    private bool isSlideshowRunning = false;

    /// <summary>
    /// Gets or sets a value indicating whether the slidshow function is
    /// currently running.
    /// </summary>
    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set => this.RaiseAndSetIfChanged(ref isSlideshowRunning, value);
    }

    private bool imagePathExists = false;

    /// <summary>
    /// Gets or sets a value indicating whether the currently selected image
    /// can still be found on disk.
    /// </summary>
    public bool ImagePathExists
    {
        get => imagePathExists;
        set => this.RaiseAndSetIfChanged(ref imagePathExists, value);
    }

    /// <summary>
    /// Starts the slideshow. A slideshow cannot be started if one is already running
    /// or if the user has not yet selected an image to preview.
    /// </summary>
    public async void StartSlideshow()
    {
        logger.Log("Starting slideshow.");
        if (IsSlideshowRunning || SelectedImage == null)
        {
            logger.Log("Slideshow will not start because it is already running or there is no image selected.");
            return;
        }

        await MessageBoxManager.GetMessageBoxStandard(
            "Slideshow.",
            "Use the escape key to stop the slideshow at any time.",
            ButtonEnum.Ok).ShowAsync();

        appState.SelectedTab = AvailableTabs.ImagePreview;

        slideshowTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(5000),
            DispatcherPriority.Normal,
            OnSlideshowTimerTick);

        WindowLookup.RequestFullScreen();
        appState.IsSlideshowRunning = true;
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
        slideshowTimer = null;
        WindowLookup.RequestRestore();
        appState.IsSlideshowRunning = false;
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

    /// <summary>
    /// Prompts the user to delete the currently selected image then deletes it. This will
    /// have no affect if no image has been selected or if the selected image cannot be
    /// found on disk. After deleting the image this will automatically switch to display
    /// the next available image.</summary>
    public async void DeleteSelectedImage()
    {
        logger.Log("Deleting currently selected image.");
        if (appState.SelectedImage == null)
        {
            return;
        }

        PathLike pathToDelete = appState.SelectedImage.Path;

        if (!pathToDelete.IsFile())
        {
            return;
        }

        logger.Log($"Prompting user to delete image at [{pathToDelete}].");
        ButtonResult result = await MessageBoxManager.GetMessageBoxStandard(
            "Confirm Delete",
            "Are you sure you want to delete this image? This action cannot be undone.",
            ButtonEnum.OkCancel).ShowAsync();

        if (result != ButtonResult.Ok)
        {
            logger.Log("User declined to delete image.");
            return;
        }

        ViewNextImage();

        pathToDelete.Delete();
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
        if (SelectedImage == null)
        {
            logger.Log("Could not change selected image because no image is currently selected.");
            return false;
        }

        int startingIndex = appState.SelectedImageIndex;
        while (true)
        {
            ImageItem? nextImage = GetNextImageInSelectedFolder(startingIndex, indexMapper);
            if (nextImage != null)
            {
                appState.SelectedImage = nextImage;
                return true;
            }

            FolderItem? nextFolder = GetNextFolder(indexMapper);
            if (nextFolder != null)
            {
                appState.SelectedFolder = nextFolder;
                if (indexMapper.Invoke(0) < 0)
                {
                    // The indexMapper is subtracting.
                    startingIndex = appState.Images.Length;
                }
                else
                {
                    // The indexMapper is adding.
                    startingIndex = -1;
                }
            }
            else
            {
                break;
            }
        }

        return false;
    }

    private ImageItem? GetNextImageInSelectedFolder(int startIndex, Func<int, int> indexMapper)
    {
        logger.Log($"Looking for next image starting from index [{startIndex}] "
            + $"in folder [{appState.SelectedFolder?.Path}].");

        int currentIndex = startIndex;
        while ((currentIndex = indexMapper.Invoke(currentIndex)) != startIndex)
        {
            if (currentIndex < 0)
            {
                logger.Log("Reached beginning of image array in selected folder.");
                return null;
            }
            else if (currentIndex >= appState.Images.Length)
            {
                logger.Log("Reached end of image array in selected folder.");
                return null;
            }

            ImageItem image = appState.Images[currentIndex];
            if (image.Path.Exists())
            {
                logger.Log($"Found image at path [{image}].");
                return image;
            }

            logger.Log($"Found image that no longer exists at path [{image.Path}].");
        }

        return null;
    }

    private FolderItem? GetNextFolder(Func<int, int> indexMapper)
    {
        logger.Log("Getting next available folder.");
        if (appState.SelectedFolder == null || appState.Folders.Length < 2)
        {
            logger.Log("Could not change selected folder because no folder "
                + "is currently selected or there are no more folders to traverse.");
            return null;
        }

        int startingIndex = appState.SelectedFolderIndex;

        int nextIndex = startingIndex;
        while ((nextIndex = indexMapper.Invoke(nextIndex)) != startingIndex)
        {
            if (nextIndex < 0)
            {
                logger.Log("Reached beginning of folders array.");
                nextIndex = appState.Folders.Length - 1;
            }
            else if (nextIndex >= appState.Folders.Length)
            {
                logger.Log("Reached end of folders array.");
                nextIndex = 0;
            }

            FolderItem folder = appState.Folders[nextIndex];
            if (PathLookup.GetSupportedImagesInFolder(folder.Path).Length > 0)
            {
                logger.Log($"Found folder at path [{folder.Path}].");
                return folder;
            }

            logger.Log($"Found folder that either doesn't exist or contains no images at [{folder.Path}]");
        }

        logger.Log("No next folder could be found that is valid.");

        return null;
    }
}
