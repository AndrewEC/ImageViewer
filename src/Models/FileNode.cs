namespace ImageViewer.Models;

using System.Collections.ObjectModel;

public sealed class FileNode
{
    public static readonly string LoadingPlaceholderTitle = "#Loading...#";

    public FileNode(string title, FileResource resource, ObservableCollection<FileNode> subNodes)
    {
        Title = title;
        Resource = resource;
        SubNodes = subNodes;
        IsExpanded = ShouldStartExpanded();
    }

    public string Title { get; set; }

    public FileResource Resource { get; set; }

    public ObservableCollection<FileNode> SubNodes { get; set; }

    public bool IsExpanded { get; private set; }

    public bool ContainsOnlyLoadMorePlaceholder() => SubNodes.Count == 1 && SubNodes[0].Title == LoadingPlaceholderTitle;

    private bool ShouldStartExpanded() => SubNodes.Count > 0 && !ContainsOnlyLoadMorePlaceholder();
}