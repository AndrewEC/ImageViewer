namespace ImageViewer.ViewModels;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageViewer.Log;
using ImageViewer.Models;
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
        appState.PropertyChanged += OnAppStateChanged;
        SelectFolderCommand = ReactiveCommand.Create<string, Task>(SelectFolder);
    }

    /// <summary>
    /// Gets the command to be invoked when the user selects a folder.
    /// </summary>
    public ReactiveCommand<string, Task> SelectFolderCommand { get; }

    private FolderItem[] folders = [];

    /// <summary>
    /// Gets or sets the array of folders the user can select from.
    /// </summary>
    public FolderItem[] Folders
    {
        get => folders;
        set => this.RaiseAndSetIfChanged(ref folders, value);
    }

    /// <summary>
    /// Attempts to lookup and set the currently selected folder to the folder
    /// whose absolute path matches the input absolute path.
    /// </summary>
    /// <param name="folderPath">The absolute path to the folder the user selected.</param>
    /// <returns>An async task.</returns>
    public async Task SelectFolder(string folderPath)
    {
        logger.Log($"Selecting folder from path [{folderPath}]");

        FolderItem? folder = Folders.Where(folder => folder.AbsolutePath == folderPath).FirstOrDefault();
        if (!Directory.Exists(folderPath) || folder == null)
        {
            logger.Log("The specified folder can no longer be found.");
            await MessageBoxManager.GetMessageBoxStandard(
                "Folder not found.",
                "Folder could not be opened because it could no longer be found. It may have been moved or deleted.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        appState.SelectedFolder = folder;
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
                Folders = appState.Folders;
                break;
        }
    }
}