namespace ImageViewer.ViewModels;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ImageView.Util;
using ImageViewer.Log;
using ImageViewer.Models;
using ImageViewer.Util;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

/// <summary>
/// The view model for the entire window.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Unecessary Rule")]
public partial class MainWindowViewModel : ReactiveObject
{
    private readonly AppStateProperties appState = new();
    private readonly ConsoleLogger<MainWindowViewModel> logger = new();
    private readonly WatcherProxy watcherProxy;

    /// <summary>
    /// Initializes the view model.
    /// </summary>
    /// <param name="mainWindow">The main window object to register the closing event on.</param>
    public MainWindowViewModel(MainWindow mainWindow)
    {
        mainWindow.Closing += (sender, e) =>
        {
            logger.Log("Window closing. Disposing of image cache.");
            ImageCache.Instance.Dispose();
        };

        watcherProxy = new(appState);

        appState.PropertyChanged += EventBuilder.CreatePropertyChangeConsumer(
            appState,
            new()
            {
                { nameof(appState.SelectedTab), () => SelectedTabIndex = (int)appState.SelectedTab },
                { nameof(appState.SelectedRootFolder), () => watcherProxy.StartFileSystemWatcher(appState.SelectedRootFolder) },
                { nameof(appState.IsSlideshowRunning), () => IsSlideshowRunning = appState.IsSlideshowRunning },
            });

        ImagePreviewDataContext = new(appState);
        FolderPreviewDataContext = new(appState);
        FolderListDataContext = new(appState);

        OpenRootFolderCommand = ReactiveCommand.Create(OpenRootFolder);
        OpenImageCommand = ReactiveCommand.Create(OpenImage);
        ShowImagePathCommand = ReactiveCommand.Create(ShowImagePath);
        ShowImageInFolderCommand = ReactiveCommand.Create(ShowImageInFolder);
        OpenFolderInExplorerCommand = ReactiveCommand.Create(OpenFolderInExplorer);

        SelectFolderCommand = ReactiveCommand.Create<string, Task>(FolderListDataContext.SelectFolder);

        ViewImageCommand = ReactiveCommand.Create<string, Task>(FolderPreviewDataContext.ViewImage);
        ViewNextImageCommand = ReactiveCommand.Create(ImagePreviewDataContext.ViewNextImage);
        ViewPreviousImageCommand = ReactiveCommand.Create(ImagePreviewDataContext.ViewPreviousImage);
        DeleteSelectedImageCommand = ReactiveCommand.Create(ImagePreviewDataContext.DeleteSelectedImage);

        StartSlideshowCommand = ReactiveCommand.Create(ImagePreviewDataContext.StartSlideshow);
        StopSlideshowCommand = ReactiveCommand.Create(ImagePreviewDataContext.StopSlideshow);

        RxApp.MainThreadScheduler.Schedule(LoadInitialFolder);
    }

    /// <summary>
    /// Gets the data context for the image preview view.
    /// </summary>
    public ImagePreviewViewModel ImagePreviewDataContext { get; }

    /// <summary>
    /// Gets the data context for the folder preview view.
    /// </summary>
    public FolderPreviewViewModel FolderPreviewDataContext { get; }

    /// <summary>
    /// Gets the data context for the folder list view.
    /// </summary>
    public FolderListViewModel FolderListDataContext { get; }

    /// <summary>
    /// Gets the reactive command tied with the "Select Folder" button.
    /// Launches a dialog to allow the user to select the root folder containing
    /// the nested folders and images to view.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenRootFolderCommand { get; }

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

    /// <summary>
    /// Gets the command to be invoked when the user clicks the show image path
    /// option from the dropdown menu.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowImagePathCommand { get; }

    /// <summary>
    /// Gets the command to be invoked when the user clicks the show in explorer
    /// option from the dropdown menu.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowImageInFolderCommand { get; }

    /// <summary>
    /// Gets the command to be invoked when the user clicks on the delete image
    /// option in the menu.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteSelectedImageCommand { get; }

    /// <summary>
    /// Gets the command to be invoked when the user clicks on the open in
    /// explorer menu option.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenFolderInExplorerCommand { get; }

    private int selectedTabIndex = (int)AvailableTabs.FolderList;

    /// <summary>
    /// Gets or sets the currently selected tab in the main tab view.
    /// </summary>
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
            ImagePreviewDataContext.StopSlideshow();
            appState.SelectedTab = (AvailableTabs)value;
        }
    }

    private bool isSlideshowRunning = false;

    /// <summary>
    /// Gets or sets a value indicating whether the slideshow is currently playing.
    /// </summary>
    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set => this.RaiseAndSetIfChanged(ref isSlideshowRunning, value);
    }

    private static PathLike GetStartPathFromLaunchArguments()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return PathLike.Empty();
        }

        string[]? arguments = desktop.Args;

        if (arguments == null || arguments.Length != 1 || !Path.IsPathFullyQualified(arguments[0]))
        {
            return PathLike.Empty();
        }

        return new PathLike(arguments[0]);
    }

    private async void ShowImagePath()
    {
        logger.Log("Showing path to currently selected image.");
        if (appState.SelectedImage == null)
        {
            logger.Log("Cannot show path to currently selected image because no image has been selected.");
            return;
        }

        await MessageBoxManager.GetMessageBoxStandard(
                "Image Path",
                appState.SelectedImage.Path.PathString,
                ButtonEnum.Ok).ShowAsync();
    }

    private void ShowImageInFolder()
    {
        logger.Log("Showing currently selected image in explorer.");
        if (appState.SelectedImage == null)
        {
            logger.Log("Selected image cannot be shown because no image has been selected.");
            return;
        }

        appState.SelectedTab = AvailableTabs.ImagePreview;

        string imagePath = appState.SelectedImage.Path.PathString.Replace("/", "\\");
        string arguments = $"/select,\"{imagePath}\"";
        logger.Log($"Starting explorer process with arguments [{arguments}]");
        Process.Start("explorer.exe", arguments);
    }

    private void OpenFolderInExplorer()
    {
        logger.Log("Opening current folder in explorer.");
        if (appState.SelectedFolder == null)
        {
            logger.Log("Selected folder cannot be opened because no folder has been selected.");
            return;
        }

        appState.SelectedTab = AvailableTabs.FolderPreview;

        logger.Log($"Opening folder [{appState.SelectedFolder.Path.PathString}].");
        Process.Start("explorer.exe", appState.SelectedFolder.Path.PathString);
    }

    private async void OpenRootFolder()
    {
        logger.Log("Prompting user to select root folder.");
        string selectedFolder = await ItemPicker.PromptForFolder();
        if (selectedFolder == string.Empty)
        {
            logger.Log("User closed root folder picker.");
            return;
        }

        appState.SelectedRootFolder = new PathLike(selectedFolder);
    }

    private async void OpenImage()
    {
        logger.Log("Prompting user to select image.");
        string imagePath = await ItemPicker.PromptForImage();
        if (imagePath == string.Empty)
        {
            logger.Log("User closed image file picker.");
            return;
        }

        await NavigateDirectlyToImage(new PathLike(imagePath));
    }

    private async Task NavigateDirectlyToImage(PathLike imagePath)
    {
        logger.Log($"Navigating directly to image with path [{imagePath}]");
        PathLike parentDirectory = imagePath.GetParentDirectory();

        if (parentDirectory == null)
        {
            logger.Log("Could not navigate directly to image because the parent folder of the image could not be found.");
            return;
        }

        appState.SelectedRootFolder = parentDirectory;
        await FolderListDataContext.SelectFolder(parentDirectory.PathString ?? string.Empty);
        await FolderPreviewDataContext.ViewImage(imagePath.PathString ?? string.Empty);
    }

    private async void LoadInitialFolder()
    {
        logger.Log("Initializing app.");
        PathLike startPath = GetStartPathFromLaunchArguments();
        if (startPath.IsDirectory())
        {
            logger.Log($"Initializing app with pre-selected root folder of [{startPath}]");
            appState.SelectedRootFolder = startPath;
        }
        else if (startPath.IsFile())
        {
            await NavigateDirectlyToImage(startPath);
        }
    }
}
