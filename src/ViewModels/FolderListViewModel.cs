namespace ImageViewer.ViewModels;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ImageView.Util;
using ImageViewer.Log;
using ImageViewer.Models;
using ImageViewer.Util;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

/// <summary>
/// The view model for <see cref="FolderListView"/>.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Reviewed.")]
public class FolderListViewModel : ReactiveObject
{
    private readonly ConsoleLogger<FolderListView> logger = new();
    private readonly AppStateProperties appState;

    /// <summary>
    /// Initializes the folder list view model.
    /// </summary>
    /// <param name="appState">The root application state. This view model will listen
    /// for changes on the <see cref="AppStateProperties.Folders"/> property.</param>
    public FolderListViewModel(AppStateProperties appState)
    {
        this.appState = appState;

        SelectFolderCommand = ReactiveCommand.Create<string, Task>(SelectFolder);

        appState.PropertyChanged += EventBuilder.CreatePropertyChangeConsumer(
            appState,
            new()
            {
                { nameof(appState.Folders), () => Folders = appState.Folders },
            });
    }

    /// <summary>
    /// Gets the command to be invoked when the user selects a folder.
    /// </summary>
    public ReactiveCommand<string, Task> SelectFolderCommand { get; }

    private FolderResource[] folders = [];

    /// <summary>
    /// Gets or sets the array of folders the user can select from.
    /// </summary>
    public FolderResource[] Folders
    {
        get => folders;
        set => this.RaiseAndSetIfChanged(ref folders, value);
    }

    /// <summary>
    /// Attempts to look up and set the currently selected folder to the folder
    /// whose absolute path matches the input absolute path.
    /// </summary>
    /// <param name="folderPath">The absolute path to the folder the user selected.</param>
    /// <returns>An async task.</returns>
    public async Task SelectFolder(string folderPath)
    {
        logger.Log($"Selecting folder from path [{folderPath}]");

        FolderResource? folder = Folders.FirstByPath(new PathLike(folderPath));
        logger.Log($"Found folder [{folder?.DisplayName}]");
        if (!(folder?.Path.IsDirectory() ?? false))
        {
            logger.Log("The specified folder can no longer be found.");
            await MessageBoxManager.GetMessageBoxStandard(
                "Folder not found.",
                "Folder could not be opened because it could no longer be found. It may have been moved or deleted.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        appState.SelectedFolder = folder;
        appState.SelectedTab = AvailableTabs.FolderPreview;
    }
}