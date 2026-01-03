namespace ImageViewer.ViewModels;

using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using ImageViewer.Models;
using ImageViewer.Utils;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public partial class ExplorerViewModel : ViewModelBase
{
    private readonly ConsoleLogger<ExplorerViewModel> logger = new();
    private readonly TreeView treeView;
    private FileNode? lastSelectedNode;
    private FileNode? lastExpandedOrCollapsedNode;

    public ExplorerViewModel(ExplorerView parentView)
    {
        UpdatePathCommand = ReactiveCommand.Create(UpdatePath);
        TreeNodes = AppState.Instance.FileNodeTree;

        treeView = parentView.FindControl<TreeView>("FolderTree")!;
        treeView.ContainerPrepared += OnTreeViewContainerPrepared;
        treeView.SelectionChanged += OnTreeViewSelectionChanged;

        Handlers.RegisterPropertyChangeHandler(AppState.Instance, new()
        {
            {
                nameof(AppState.Instance.FileNodeTree),
                () => TreeNodes = AppState.Instance.FileNodeTree
            },
            {
                nameof(AppState.Instance.SelectedFolder),
                () => CurrentPath = AppState.Instance.SelectedFolder?.Path.PathString ?? string.Empty
            },
        });
    }

    public ReactiveCommand<Unit, Unit> UpdatePathCommand { get; }

    private ObservableCollection<FileNode> treeNodes = AppState.Instance.FileNodeTree;

    public ObservableCollection<FileNode> TreeNodes
    {
        get => treeNodes;
        set => this.RaiseAndSetIfChanged(ref treeNodes, value);
    }

    private string currentPath = string.Empty;

    public string CurrentPath
    {
        get => currentPath;
        set => this.RaiseAndSetIfChanged(ref currentPath, value);
    }

    private void OnTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1 && e.AddedItems[0] is FileNode node)
        {
            logger.Log($"User selected folder: [{node.Resource.Path.PathString}]");

            lastExpandedOrCollapsedNode = null;
            lastSelectedNode = node;
            AppState.Instance.SelectedFolder = node.Resource;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Every time the node collection of the tree view is updated (this occurrs whenever the
    /// user expands or collapses a node), this method will be invoked.
    /// This method will handle keeping the last node the user selected or expanded in view
    /// so the user does not have to manually scroll back to the node every time
    /// any change occurs to the tree view.
    /// </summary>
    private void OnTreeViewContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        TreeViewItem treeViewItem = (TreeViewItem)e.Container;
        treeViewItem.PropertyChanged += OnTreePropertyChanged;
        if (treeViewItem?.DataContext is FileNode treeViewModel)
        {
            treeViewItem.ContainerPrepared += OnTreeViewContainerPrepared;

            if (IsLastSelectedNode(treeViewModel)
                || IsLastExpandedOrCollapsedNode(treeViewModel)
                || IsNodeMatchingCurrentPath(treeViewModel))
            {
                treeView.SelectedItem = treeViewModel;
                treeView.ScrollIntoView(treeViewModel);
            }
        }
    }

    private bool IsLastSelectedNode(FileNode treeViewModel)
        => lastSelectedNode != null && treeViewModel.Resource.Path.Equals(lastSelectedNode.Resource.Path);

    private bool IsLastExpandedOrCollapsedNode(FileNode treeViewModel)
        => lastExpandedOrCollapsedNode != null && treeViewModel.Resource.Path.Equals(lastExpandedOrCollapsedNode.Resource.Path);

    private bool IsNodeMatchingCurrentPath(FileNode treeViewModel)
        => CurrentPath != string.Empty && treeViewModel.Resource.Path.PathString == currentPath;

    /// <summary>
    /// This method will be invoked whenever a property of a tree node is updated.
    /// This method will specifically handle what occurrs when the IsExpanded property
    /// changes.
    /// </summary>
    private void OnTreePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not TreeViewItem item
            || e.Property.Name != nameof(TreeViewItem.IsExpanded))
        {
            return;
        }

        FileNode node = (item.DataContext as FileNode)!;
        lastSelectedNode = null;
        lastExpandedOrCollapsedNode = null;
        if (node.IsExpanded)
        {
            logger.Log($"User expanded node: [{node.Resource.Path.PathString}]");
            AppState.Instance.CollapseNode(node);
        }
        else if (node.ContainsOnlyLoadMorePlaceholder())
        {
            logger.Log($"User collapsed node: [{node.Resource.Path.PathString}]");
            AppState.Instance.ExpandNode(node);
        }
    }

    private async void UpdatePath()
    {
        logger.Log($"Updating path to: [{CurrentPath}].");

        PathLike target = new(CurrentPath);
        if (!target.IsDirectory())
        {
            logger.Log("Requested path could not be found.");
            await MessageBoxManager.GetMessageBoxStandard(
                "Invalid Path",
                "The specified path could not be found. Please check the path points to a directoy and try again.",
                ButtonEnum.Ok)
                .ShowAsync();
            return;
        }

        AppState.Instance.LoadStartingPath(target);
    }
}
