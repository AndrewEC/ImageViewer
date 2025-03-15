namespace ImageViewer.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        set
        {
            this.RaiseAndSetIfChanged(ref selectedImage, value);
            IsImageSelected = value != null;
            ImagePathExists = File.Exists(value?.AbsolutePath ?? string.Empty);
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
            "Use the space key to stop the slideshow at any time.",
            ButtonEnum.Ok).ShowAsync();

        slideshowTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(2000),
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
        if (IsSlideshowRunning)
        {
            return;
        }

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
        if (IsSlideshowRunning)
        {
            return;
        }

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

        string pathToDelete = appState.SelectedImage.AbsolutePath;

        if (!Path.Exists(pathToDelete))
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

        File.Delete(pathToDelete);
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

        int index = Array.IndexOf(appState.Images, SelectedImage);
        if (!appState.Images.IsValidIndex(index))
        {
            logger.Log($"Could not find index of current image. Index lookup return [{index}].");
            return false;
        }

        ImageItem? nextImage = SearchForValidImage(indexMapper.Invoke(index), indexMapper, 0);

        if (nextImage != null)
        {
            appState.SelectedImage = nextImage;
        }

        return nextImage != null;
    }

    // Assumption is that at least one folder from the appState.Images array will
    // be valid. Therefore, no check for infinite recursion will be done.
    private ImageItem? SearchForValidImage(int currentIndex, Func<int, int> indexMapper, int recursiveDepth)
    {
        if (recursiveDepth > 50)
        {
            logger.Log("Search for image failed. Reached max recursive depth.");
            return null;
        }

        if (currentIndex < 0)
        {
            logger.Log("Reached beginning image array in selected folder.");
            if (!ChangeActiveFolder(indexMapper))
            {
                return null;
            }

            return SearchForValidImage(appState.Images.Length - 1, indexMapper, ++recursiveDepth);
        }
        else if (currentIndex >= appState.Images.Length)
        {
            logger.Log("Reached end of image array in selected folder.");
            if (!ChangeActiveFolder(indexMapper))
            {
                return null;
            }

            return SearchForValidImage(0, indexMapper, ++recursiveDepth);
        }

        ImageItem image = appState.Images[currentIndex];
        if (File.Exists(image.AbsolutePath))
        {
            return image;
        }

        logger.Log($"Found image item that no longer exists at path [{image.AbsolutePath}].");

        return SearchForValidImage(indexMapper.Invoke(currentIndex), indexMapper, ++recursiveDepth);
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

        FolderItem? nextFolder = SearchForValidFolder(indexMapper.Invoke(index), indexMapper, 0);
        if (nextFolder == null)
        {
            return false;
        }

        appState.SelectedFolder = nextFolder;
        return true;
    }

    // Assumption is that at least one folder from the appState.Folders array will
    // be valid. Therefore, no check for infinite recursion will be done.
    private FolderItem? SearchForValidFolder(int currentIndex, Func<int, int> indexMapper, int recursiveDepth)
    {
        if (recursiveDepth > 50)
        {
            logger.Log("Search for folder failed. Reached max recursive depth.");
            return null;
        }

        if (currentIndex < 0)
        {
            logger.Log("Reached beginning of first folder in folders array.");
            return SearchForValidFolder(appState.Folders.Length - 1, indexMapper, ++recursiveDepth);
        }
        else if (currentIndex >= appState.Folders.Length)
        {
            logger.Log("Reached end of last folder in folders array.");
            return SearchForValidFolder(0, indexMapper, ++recursiveDepth);
        }

        FolderItem folder = appState.Folders[currentIndex];
        if (PathLookup.GetSupportedImagesInFolder(folder.AbsolutePath).Length > 0)
        {
            logger.Log($"Found value folder at path [{folder.AbsolutePath}].");
            return folder;
        }

        logger.Log($"Found folder that either doesn't exist or contains no images at [{folder.AbsolutePath}]");

        return SearchForValidFolder(indexMapper(currentIndex), indexMapper, ++recursiveDepth);
    }

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != appState)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(appState.SelectedImage):
                SelectedImage = appState.SelectedImage;
                break;
            case nameof(appState.IsSlideshowRunning):
                IsSlideshowRunning = appState.IsSlideshowRunning;
                break;
        }
    }
}
