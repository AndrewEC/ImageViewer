namespace ImageViewer.ViewModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using ImageViewer.Injection;
using ImageViewer.Models;
using ImageViewer.State;
using ImageViewer.Utils;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public partial class FolderPreviewViewModel : ViewModelBase
{
    private readonly ConsoleLogger<FolderPreviewViewModel> logger = new();
    private readonly IAppState appState;
    private readonly ScrollViewer imageScrollViewer;

    public FolderPreviewViewModel(FolderPreview parentView)
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

        imageScrollViewer = parentView.FindControl<ScrollViewer>("ImageScrollViewer")!;
        imageScrollViewer.ScrollChanged += OnImageScrollChanged;
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
            logger.Log("Image was successfully deleted.");
            appState.RemoveImageFromSelectedFolder(resourceToDelete);
        }
    }

    private void ShowInExplorer(PathLike path)
    {
        logger.Log($"Opening image in explorer: [{path.PathString}]");
        Process.Start("explorer.exe", "/select," + path.PathString);
    }

    /// <summary>
    /// Enables lazy loading of the Image thumbnail. By default, the Source property of
    /// the Image controls in this view are null. When the scroll position changes we
    /// will check to see if the image is in view. If it is then we will update the
    /// Source property of the Image control to be the proper thumbnail image triggering
    /// a load of said thumbnail from disk.
    /// </summary>
    private async void OnImageScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        IEnumerable<StackPanel> imagePanels = imageScrollViewer.GetVisualDescendants().OfType<StackPanel>();
        MainWindow window = GetMainWindow();
        foreach (StackPanel imagePanel in imagePanels)
        {
            ContentPresenter container = (imagePanel.Parent as ContentPresenter)!;
            double yPosition = container.Bounds.Top - imageScrollViewer.Offset.Y;
            if (yPosition < window.Bounds.Height)
            {
                Image image = imagePanel.GetVisualDescendants().OfType<Image>().First();
                if (image.Source == null)
                {
                    ImageResource imageResource = (container.DataContext as ImageResource)!;
                    image.Source = await imageResource.Thumbnail;
                }
            }
        }
    }

    private static MainWindow GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return (lifetime.MainWindow as MainWindow)!;
        }
#pragma warning disable CA2201
        throw new Exception("Could not find MainWindow.");
#pragma warning restore CA2201
    }
}