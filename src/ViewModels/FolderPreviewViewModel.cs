namespace ImageViewer.ViewModels;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ImageViewer.Models;
using ImageViewer.Util;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

/// <summary>
/// The view model for <see cref="FolderPreviewView"/>.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Reviewed.")]
public class FolderPreviewViewModel : ReactiveObject
{
    private readonly AppStateProperties appState;

    /// <summary>
    /// Initializes the folder prview view model.
    /// </summary>
    /// <param name="appState">The root application state. This view model will listen
    /// for changes on the <see cref="AppStateProperties.Images"/> property.</param>
    public FolderPreviewViewModel(AppStateProperties appState)
    {
        this.appState = appState;
        appState.PropertyChanged += HelperExtensions.CreatePropertyChangeConsumer(
            appState,
            new()
            {
                { nameof(appState.Images), () => Images = appState.Images },
                { nameof(appState.SelectedFolder), () => SelectedFolder = appState.SelectedFolder },
            });

        ViewImageCommand = ReactiveCommand.Create<string, Task>(ViewImage);
    }

    /// <summary>
    /// Gets the command to be invoked when the user clicks on an image to view.
    /// </summary>
    public ReactiveCommand<string, Task> ViewImageCommand { get; }

    private ImageItem[] images = [];

    /// <summary>
    /// Gets or sets the list of images the user can select from.
    /// </summary>
    public ImageItem[] Images
    {
        get => images;
        set => this.RaiseAndSetIfChanged(ref images, value);
    }

    private FolderItem? selectedFolder = null;

    /// <summary>
    /// Gets or sets the folder the user has selected.
    /// </summary>
    public FolderItem? SelectedFolder
    {
        get => selectedFolder;
        set => this.RaiseAndSetIfChanged(ref selectedFolder, value);
    }

    /// <summary>
    /// Attempts to lookup and set the currently selected image to the
    /// image whose absolute path matches the absolute path provided.
    /// </summary>
    /// <param name="imagePath">The absolute path of the image to lookup.</param>
    /// <returns>An async task.</returns>
    public async Task ViewImage(string imagePath)
    {
        ImageItem? image = Images.FirstByPath(new PathLike(imagePath));
        if (!(image?.Path.IsFile() ?? false))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Image not found.",
                "Image could not be opened because it could no longer be found.",
                ButtonEnum.Ok).ShowAsync();
            return;
        }

        appState.SelectedImage = image;
        appState.SelectedTab = AvailableTabs.ImagePreview;
    }
}