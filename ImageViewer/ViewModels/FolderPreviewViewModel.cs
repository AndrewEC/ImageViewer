namespace ImageViewer.ViewModels;

using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using ImageViewer.Injection;
using ImageViewer.Models;
using ImageViewer.State;
using ImageViewer.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public partial class FolderPreviewViewModel : ViewModelBase
{
    private readonly ConsoleLogger<FolderPreviewViewModel> logger = new();
    private readonly IAppState appState;

    public FolderPreviewViewModel()
    {
        appState = ServiceContainer.GetService<IAppState>();

        Handlers.RegisterPropertyChangeHandler(appState, new()
        {
            {
                nameof(appState.SelectedFolderResources),
                () => ImageResources = appState.SelectedFolderResources
            },
        });

        ImageClickedCommand = ReactiveCommand.Create<PathLike>(ImageClicked);
        DeleteImageCommand = ReactiveCommand.Create<PathLike>(DeleteImage);
        ShowInExplorerCommand = ReactiveCommand.Create<PathLike>(ShowInExplorer);
        StartSlideshowCommand = ReactiveCommand.Create<PathLike>(StartSlideshow);
    }

    public ReactiveCommand<PathLike, Unit> ImageClickedCommand { get; }

    public ReactiveCommand<PathLike, Unit> DeleteImageCommand { get; }

    public ReactiveCommand<PathLike, Unit> ShowInExplorerCommand { get; }

    public ReactiveCommand<PathLike, Unit> StartSlideshowCommand { get; }

    public List<ImageResource> ImageResources
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    private void StartSlideshow(PathLike imagePath)
    {
        ImageClicked(imagePath);
        appState.IsSlideshowRunning = true;
    }

    private void ImageClicked(PathLike imagePath)
    {
        logger.Log($"User selected image: [{imagePath.PathString}]");
        ImageResource? resource = ImageResources.Find(resource => resource.Path.Equals(imagePath));
        if (resource == null)
        {
            logger.Log("The image the user selected could not be found.");
            return;
        }

        appState.SelectedImage = resource;
    }

    private async void DeleteImage(PathLike imagePath)
    {
        logger.Log($"User requested image [{imagePath.PathString}] be deleted.");
        ImageResource? resourceToDelete = appState.FindImageWithPath(imagePath);
        if (resourceToDelete == null)
        {
            return;
        }

        var result = await MessageBoxManager.GetMessageBoxStandard(
            "Delete Image",
            "Are you sure you want to delete this image? This action cannot be undone.",
            ButtonEnum.YesNo)
            .ShowAsync();

        if (result != ButtonResult.Yes)
        {
            return;
        }

        logger.Log($"Deleting image [{resourceToDelete.Path.PathString}]");
        if (resourceToDelete.Path.Delete())
        {
            logger.Log("Image was not successfully deleted.");
            appState.RemoveImageFromSelectedFolder(resourceToDelete);
        }
    }

    private void ShowInExplorer(PathLike path)
    {
        logger.Log($"Opening image in explorer: [{path.PathString}]");
        Process.Start("explorer.exe", "/select," + path.PathString);
    }
}