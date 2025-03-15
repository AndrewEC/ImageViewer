namespace ImageViewer.ViewModels;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ImageViewer.Log;
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
    private readonly AppStateProperties appState = new();
    private readonly ConsoleLogger<MainWindowViewModel> logger = new();

    /// <summary>
    /// Initializes the view model.
    /// </summary>
    public MainWindowViewModel()
    {
        appState.PropertyChanged += OnAppStateChanged;

        ImagePreviewDataContext = new(appState);
        FolderPreviewDataContext = new(appState);
        FolderListDataContext = new(appState);

        OpenRootFolderCommand = ReactiveCommand.Create(OpenRootFolder);
        OpenImageCommand = ReactiveCommand.Create(OpenImage);
        ShowImagePathCommand = ReactiveCommand.Create(ShowImagePath);
        ShowImageInFolderCommand = ReactiveCommand.Create(ShowImageInFolder);

        SelectFolderCommand = ReactiveCommand.Create<string, Task>(FolderListDataContext.SelectFolder);

        ViewImageCommand = ReactiveCommand.Create<string, Task>(FolderPreviewDataContext.ViewImage);
        ViewNextImageCommand = ReactiveCommand.Create(ImagePreviewDataContext.ViewNextImage);
        ViewPreviousImageCommand = ReactiveCommand.Create(ImagePreviewDataContext.ViewPreviousImage);

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
        }
    }

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

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != appState)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(appState.SelectedTab):
                SelectedTabIndex = (int)appState.SelectedTab;
                break;
        }
    }

    private async void ShowImagePath()
    {
        logger.Log("Showing path to currently selected.");
        if (appState.SelectedImage == null)
        {
            logger.Log("Cannot show path to currently selected image because no image has been selected.");
            return;
        }

        await MessageBoxManager.GetMessageBoxStandard(
                "Image Path",
                appState.SelectedImage.AbsolutePath,
                ButtonEnum.Ok).ShowAsync();
    }

    private void ShowImageInFolder()
    {
        logger.Log("Showing currently selected image in folder.");
        if (appState.SelectedImage == null)
        {
            logger.Log("Selected image cannot be shown because no image has been selected.");
            return;
        }

        string imagePath = appState.SelectedImage.AbsolutePath.Replace("/", "\\");
        string arguments = $"/select,\"{imagePath}\"";
        logger.Log($"Starting explorer process with arguments [{arguments}]");
        Process.Start("explorer.exe", arguments);
    }

    private async void OpenRootFolder()
    {
        logger.Log("Prompting user to select root folder.");
        string selectedFolder = await PathPicker.PickFolder();
        if (selectedFolder == string.Empty)
        {
            logger.Log("User closed root folder picker.");
            return;
        }

        appState.SelectedRootFolder = selectedFolder;
    }

    private async void OpenImage()
    {
        logger.Log("Prompting user to select image.");
        string imagePath = await PathPicker.PickImage();
        if (imagePath == string.Empty)
        {
            logger.Log("User closed image file picker.");
            return;
        }

        await NavigateDirectlyToImage(imagePath);
    }

    private async Task NavigateDirectlyToImage(string imagePath)
    {
        logger.Log($"Navigating directly to image with path [{imagePath}]");
        string selectedFolder = Path.GetDirectoryName(imagePath) ?? string.Empty;
        string rootFolder = Path.GetDirectoryName(selectedFolder) ?? string.Empty;

        if (selectedFolder == null || rootFolder == null)
        {
            logger.Log("Could not navigate directly to path because the path is missing one or more parent folders.");
            return;
        }

        appState.SelectedRootFolder = rootFolder;
        await FolderListDataContext.SelectFolder(selectedFolder);
        await FolderPreviewDataContext.ViewImage(imagePath);
    }

    private async void LoadInitialFolder()
    {
        logger.Log("Initializing app.");
        string startPath = GetStartPathFromLaunchArguments();
        if (startPath == string.Empty)
        {
            logger.Log("No startup path provided. No default image or root folder will be selected.");
            return;
        }

        if (File.GetAttributes(startPath).HasFlag(FileAttributes.Directory))
        {
            logger.Log($"Initializing app with pre-selected root folder of [{startPath}]");
            appState.SelectedRootFolder = startPath;
        }
        else
        {
            await NavigateDirectlyToImage(startPath);
        }
    }
}
