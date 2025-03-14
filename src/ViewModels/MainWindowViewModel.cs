namespace ImageViewer.ViewModels;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ImageViewer.Models;
using ImageViewer.Pickers;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

/// <summary>
/// The view model for the entire window.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Unecessary Rule")]
public partial class MainWindowViewModel : ReactiveObject
{
    /// <summary>
    /// Initializes the view model.
    /// </summary>
    public MainWindowViewModel()
    {
        OpenRootFolderCommand = ReactiveCommand.Create(OpenRootFolder);
        OpenImageCommand = ReactiveCommand.Create(OpenImage);

        SelectFolderCommand = ReactiveCommand.Create<string, Task>(SelectFolder);
        RefreshCommand = ReactiveCommand.Create(Refresh);

        ViewImageCommand = ReactiveCommand.Create<string, Task>(ViewImage);
        ViewNextImageCommand = ReactiveCommand.Create(ViewNextImage);
        ViewPreviousImageCommand = ReactiveCommand.Create(ViewPreviousImage);

        StartSlideshowCommand = ReactiveCommand.Create(StartSlideshow);
        StopSlideshowCommand = ReactiveCommand.Create(StopSlideshow);

        RxApp.MainThreadScheduler.Schedule(LoadInitialFolder);
    }

    /// <summary>
    /// Gets the reactive command tied with the "Select Folder" button.
    /// Launches a dialog to allow the user to select the root folder containing
    /// the nested folders and images to view.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenRootFolderCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the refresh button. Allows the user
    /// to force a re-scan of the previously selected root folder. If no root
    /// folder has been selected then this should have no effect.
    /// </summary>
    public ReactiveCommand<Unit, Task> RefreshCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the click of a sub-folder within
    /// the root folder. This "opens" the folder so the images within the
    /// sub-folder can be previewed within the second tab.
    /// </summary>
    public ReactiveCommand<string, Task> SelectFolderCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the click of an image within the second
    /// image preview tab after a sub-folder has been "opened". This "opens" the image
    /// so it can be viewed in the third tab.
    /// </summary>
    public ReactiveCommand<string, Task> ViewImageCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the click of the "&gt;" button on the third
    /// tab. Allows the user to navigate to the next available image if one is
    /// available.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewNextImageCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the click of the "&lt;" button on the third
    /// tab. Allows the user to navigate to the previous available image if one is
    /// available.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewPreviousImageCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to opening a specific image and it's parent folders.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenImageCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the initiation of the slideshow to
    /// automatically walk foward through the currently available list of images.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSlideshowCommand { get; }

    /// <summary>
    /// Gets the reactive command tied to the stopping of the slideshow.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopSlideshowCommand { get; }

    private string selectedRootFolder = string.Empty;

    private FolderItem? selectedFolder = null;

    /// <summary>
    /// Gets or sets the currently "opened" sub-folder. On change this will update
    /// the <see cref="Images"/> array.
    /// </summary>
    public FolderItem? SelectedFolder
    {
        get => selectedFolder;
        set
        {
            this.RaiseAndSetIfChanged(ref selectedFolder, value);
            if (value == null)
            {
                Images = [];
                return;
            }

            Images = GetSupportedImagesInFolder(value.AbsolutePath)
                .Select(file => new ImageItem(file))
                .ToArray();
            SelectedTabIndex = (int)AvailableTabs.FolderView;
        }
    }

    private FolderItem[] folders = [];

    /// <summary>
    /// Gets or sets the list of sub-folders within the selected root folder.
    /// On change this will also set <see cref="SelectedFolder"/> to null.
    /// </summary>
    public FolderItem[] Folders
    {
        get => folders;
        set
        {
            this.RaiseAndSetIfChanged(ref folders, value);
            SelectedFolder = null;
            SelectedTabIndex = (int)AvailableTabs.FolderPreview;
        }
    }

    private int selectedTabIndex = (int)AvailableTabs.FolderPreview;

    /// <summary>
    /// Gets or sets the currently selected tab in the main tab view.
    /// </summary>
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
    }

    private ImageItem[] images = [];

    /// <summary>
    /// Gets or sets the list of images in the currently "opened" sub-folder.
    /// On change this will reset the <see cref="SelectedImage"/> to null.
    /// </summary>
    public ImageItem[] Images
    {
        get => images;
        set
        {
            this.RaiseAndSetIfChanged(ref images, value);
            SelectedImage = null;
        }
    }

    private ImageItem? selectedImage = null;

    /// <summary>
    /// Gets or sets the currently selected image.
    /// </summary>
    public ImageItem? SelectedImage
    {
        get => selectedImage;
        set
        {
            this.RaiseAndSetIfChanged(ref selectedImage, value);
            ImageSelected = value != null;
            if (value != null)
            {
                SelectedTabIndex = (int)AvailableTabs.ImageView;
            }
        }
    }

    private bool imageSelected = false;

    /// <summary>
    /// Gets or sets a value indicating whether an image has been selected and can be
    /// displayed.
    /// </summary>
    public bool ImageSelected
    {
        get => imageSelected;
        set => this.RaiseAndSetIfChanged(ref imageSelected, value);
    }

    private bool userHasControl = true;

    /// <summary>
    /// Gets or sets a value indicating whether the left and right
    /// navigation buttons should be visible to the user so they can
    /// directly control which image they are viewing.
    /// </summary>
    public bool UserHasControl
    {
        get => userHasControl;
        set => this.RaiseAndSetIfChanged(ref userHasControl, value);
    }

    private DispatcherTimer? slideshowTimer;

    private static string GetStartPathFromLaunchArguments()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return string.Empty;
        }

        string[]? arguments = desktop.Args;

        if (arguments == null || arguments.Length != 1 || !Path.IsPathFullyQualified(arguments[0]))
        {
            return string.Empty;
        }

        return arguments[0];
    }

    private async void StartSlideshow()
    {
        if (!UserHasControl || SelectedImage == null)
        {
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

        UserHasControl = false;
    }

    private void StopSlideshow()
    {
        if (slideshowTimer == null)
        {
            return;
        }

        slideshowTimer.Stop();
        UserHasControl = true;
    }

    private void OnSlideshowTimerTick(object? sender, EventArgs e)
    {
        ViewNextImage();
    }

    private async void OpenRootFolder()
    {
        string selectedFolder = await PathPicker.PickFolder();
        if (selectedFolder == string.Empty)
        {
            return;
        }

        selectedRootFolder = selectedFolder;
        await Refresh();
    }

    private FolderItem[] MapToFolderItems(string[] subDirectories)
        => subDirectories.Select(MapToFolderItem)
            .Where(folder => folder != null)
            .OfType<FolderItem>()
            .ToArray();

    private FolderItem? MapToFolderItem(string directory)
    {
        string[] files = GetSupportedImagesInFolder(directory);

        if (files.Length == 0)
        {
            return null;
        }

        string displayName = Path.GetFileName(directory);
        return new(directory, displayName, files[0]);
    }

    private string[] GetSupportedImagesInFolder(string directory) => Directory.GetFiles(directory)
            .Where(file => PathPicker.SupportedImageExtensions.Contains(Path.GetExtension(file)))
            .ToArray();

    private async Task Refresh()
    {
        if (selectedRootFolder == string.Empty)
        {
            return;
        }

        if (!Directory.Exists(selectedRootFolder))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Folder not found.",
                "Folder could not be opened because it could no longer be found.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        string[] subDirectories = Directory.GetDirectories(selectedRootFolder);
        Folders = MapToFolderItems(subDirectories);
    }

    private async Task SelectFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Folder not found.",
                "Folder could not be opened because it could no longer be found.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        FolderItem? folder = Folders.Where(folder => folder.AbsolutePath == folderPath).FirstOrDefault();
        if (folder == null)
        {
            return;
        }

        SelectedFolder = folder;
    }

    private async Task ViewImage(string imagePath)
    {
        string imageFullPath = Path.GetFullPath(imagePath);
        ImageItem? image = Images.Where(image => Path.GetFullPath(image.AbsolutePath) == imageFullPath).FirstOrDefault();
        if (image == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Image not found.",
                "Image could not be opened because it could no longer be found.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        SelectedImage = image;
    }

    private bool ChangeActiveImage(Func<int, int> indexMapper)
    {
        if (SelectedImage == null)
        {
            return false;
        }

        int index = Array.IndexOf(Images, SelectedImage);
        if (!Images.IsValidIndex(index))
        {
            return false;
        }

        int nextIndex = indexMapper.Invoke(index);
        if (Images.IsValidIndex(nextIndex))
        {
            SelectedImage = Images[nextIndex];
            return true;
        }

        if (!ChangeActiveFolder(indexMapper))
        {
            return false;
        }

        if (nextIndex < index)
        {
            SelectedImage = Images[^1];
        }
        else
        {
            SelectedImage = Images[0];
        }

        return true;
    }

    private bool ChangeActiveFolder(Func<int, int> indexMapper)
    {
        if (SelectedFolder == null)
        {
            return false;
        }

        int index = Array.IndexOf(Folders, SelectedFolder);
        if (!Folders.IsValidIndex(index))
        {
            return false;
        }

        int nextIndex = indexMapper.Invoke(index);
        if (!Folders.IsValidIndex(nextIndex))
        {
            return false;
        }

        SelectedFolder = Folders[nextIndex];
        return true;
    }

    private void ViewNextImage()
    {
        if (!ChangeActiveImage((index) => index + 1))
        {
            ViewFirstImage();
        }
    }

    private void ViewPreviousImage()
    {
        if (!ChangeActiveImage((index) => index - 1))
        {
            ViewLastImage();
        }
    }

    private void ViewFirstImage()
    {
        SelectedFolder = Folders[0];
        SelectedImage = Images[0];
    }

    private void ViewLastImage()
    {
        SelectedFolder = Folders[^1];
        SelectedImage = Images[^1];
    }

    private async void OpenImage()
    {
        string imagePath = await PathPicker.PickImage();
        if (imagePath == string.Empty)
        {
            return;
        }

        await NavigateDirectlyToImage(imagePath);
    }

    private async Task NavigateDirectlyToImage(string imagePath)
    {
        string selectedFolder = Path.GetDirectoryName(imagePath) ?? string.Empty;
        string rootFolder = Path.GetDirectoryName(selectedFolder) ?? string.Empty;

        if (selectedFolder == null || rootFolder == null)
        {
            return;
        }

        selectedRootFolder = rootFolder;
        await Refresh();
        await SelectFolder(selectedFolder);
        await ViewImage(imagePath);
    }

    private async void LoadInitialFolder()
    {
        string startPath = GetStartPathFromLaunchArguments();
        if (startPath == string.Empty)
        {
            return;
        }

        if (File.GetAttributes(startPath).HasFlag(FileAttributes.Directory))
        {
            selectedRootFolder = startPath;
            await Refresh();
        }
        else
        {
            await NavigateDirectlyToImage(startPath);
        }
    }
}
