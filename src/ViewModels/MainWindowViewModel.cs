namespace ImageViewer.ViewModels;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ImageViewer.Models;
using ImageViewer.Pickers;
using ReactiveUI;

/// <summary>
/// The view model for the entire window.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Unecessary Rule")]
public partial class MainWindowViewModel : ReactiveObject
{
    private readonly AppStateProperties appState = new();

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

    private int selectedTabIndex = (int)AvailableTabs.FolderList;

    /// <summary>
    /// Gets or sets the currently selected tab in the main tab view.
    /// </summary>
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
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
            case nameof(appState.Folders):
                SelectedTabIndex = (int)AvailableTabs.FolderList;
                break;
            case nameof(appState.SelectedFolder):
                if (appState.SelectedFolder != null)
                {
                    SelectedTabIndex = (int)AvailableTabs.FolderPreview;
                }

                break;
            case nameof(appState.SelectedImage):
                if (appState.SelectedImage != null)
                {
                    SelectedTabIndex = (int)AvailableTabs.ImagePreview;
                }

                break;
        }
    }

    private async void OpenRootFolder()
    {
        string selectedFolder = await PathPicker.PickFolder();
        if (selectedFolder == string.Empty)
        {
            return;
        }

        appState.SelectedRootFolder = selectedFolder;
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

        appState.SelectedRootFolder = rootFolder;
        await FolderListDataContext.SelectFolder(selectedFolder);
        await FolderPreviewDataContext.ViewImage(imagePath);
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
            appState.SelectedRootFolder = startPath;
        }
        else
        {
            await NavigateDirectlyToImage(startPath);
        }
    }
}
