namespace ImageViewer.Core.Models;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ImageViewer.Core.Utils;

public sealed class AppState : INotifyPropertyChanged
{
    private static readonly ConsoleLogger<AppState> logger = new();
    public static readonly AppState Instance = new();

    private List<FileResource> rootResources = [];
    private ObservableCollection<FileNode> fileNodeTree = [];

    private AppState() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isSlideshowRunning;

    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set
        {
            isSlideshowRunning = value;
            logger.Log($"[{nameof(IsSlideshowRunning)}] value updated to [{value}].");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSlideshowRunning)));
        }
    }

    public ObservableCollection<FileNode> FileNodeTree
    {
        get => fileNodeTree;
        private set
        {
            fileNodeTree = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileNodeTree)));
        }
    }

    private FileResource? selectedFolder;

    public FileResource? SelectedFolder
    {
        get => selectedFolder;
        set
        {
            selectedFolder = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFolder)));
            SelectedFolderResources = Scan.GetImageResourcesInFolder(selectedFolder);
        }
    }

    private List<ImageResource> selectedFolderResources = [];

    public List<ImageResource> SelectedFolderResources
    {
        get => selectedFolderResources;
        set
        {
            selectedFolderResources = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFolderResources)));
            SelectedImage = null;
        }
    }

    private ImageResource? selectedImage;

    public ImageResource? SelectedImage
    {
        get => selectedImage;
        set
        {
            selectedImage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImage)));
            if (value != null)
            {
                SelectedTabIndex = (int)Tabs.ImagePreview;
            }
        }
    }

    private int selectedTabIndex;

    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set
        {
            selectedTabIndex = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTabIndex)));
        }
    }

    public void RemoveCurrentlySelectedImageFromFolderResources()
        => RemoveImageFromSelectedFolder(SelectedImage);

    public void RemoveImageFromSelectedFolder(ImageResource? imageResource)
    {
        if (imageResource == null)
        {
            return;
        }

        SelectedFolderResources = [.. SelectedFolderResources.Where(r => !r.Path.Equals(imageResource.Path))];

        if (SelectedFolderResources.Count > 0)
        {
            if (SelectedImage != null && SelectedImage.Path.Equals(imageResource.Path))
            {
                SelectedImage = SelectedFolderResources[0];
            }
        }
        else
        {
            SelectedImage = null;
        }
    }

    public void LoadStartingPath(PathLike startingPath)
    {
        SelectedTabIndex = (int)Tabs.Folders;
        SelectedImage = null;
        SelectedFolder = null;

        rootResources = Scan.GetDriveRootResources();
        Scan.ExpandPath(startingPath, rootResources);

        SelectedFolder = Scan.FindFileResourceMatchingPath(startingPath, rootResources);

        RebuildFileNodeTree();
    }

    public void ExpandNode(FileNode node)
    {
        node.Resource.Expand();
        RebuildFileNodeTree();
    }

    public void CollapseNode(FileNode node)
    {
        node.Resource.Collapse();
        RebuildFileNodeTree();
    }

    private void RebuildFileNodeTree()
        => FileNodeTree = [.. rootResources.Select(resource => resource.ToNodeTree())];

    public ImageResource? FindImageWithPath(PathLike path)
    {
        if (SelectedFolderResources.Count == 0)
        {
            return null;
        }

        return SelectedFolderResources.Find(resource => resource.Path.Equals(path));
    }
}