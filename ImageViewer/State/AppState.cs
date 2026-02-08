namespace ImageViewer.Models;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ImageViewer.Utils;

public sealed class AppState : INotifyPropertyChanged
{
    public static readonly AppState Instance = new();

    private List<FileResource> rootResources = [];

    private AppState() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSlideshowRunning
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSlideshowRunning)));
        }
    }

    public ObservableCollection<FileNode> FileNodeTree
    {
        get;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileNodeTree)));
        }
    } = [];

    public FileResource? SelectedFolder
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFolder)));
            SelectedFolderResources = Scan.GetImageResourcesInFolder(value);
        }
    }

    public List<ImageResource> SelectedFolderResources
    {
        get;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFolderResources)));
            SelectedImage = null;
        }
    } = [];

    public ImageResource? SelectedImage
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImage)));
            if (value != null)
            {
                SelectedTabIndex = (int)Tabs.ImagePreview;
            }
        }
    }

    public int SelectedTabIndex
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTabIndex)));
        }
    }

    public void RemoveCurrentlySelectedImageFromFolderResources()
        => RemoveImageFromSelectedFolder(SelectedImage);

    public void RemoveImageFromSelectedFolder(ImageResource? resourceToRemove)
    {
        if (resourceToRemove == null)
        {
            return;
        }

        SelectedFolderResources = [.. SelectedFolderResources.Where(r => !r.Path.Equals(resourceToRemove.Path))];

        if (SelectedFolderResources.Count > 0)
        {
            if (SelectedTabIndex == (int)Tabs.ImagePreview
                && SelectedImage != null
                && SelectedImage.Path.Equals(resourceToRemove.Path))
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

        RebuildFileNodeTree();

        SelectedFolder = Scan.FindFileResourceMatchingPath(startingPath, rootResources);
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