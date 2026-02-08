namespace ImageViewer.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ImageViewer.Injection;
using ImageViewer.Models;
using ImageViewer.State;
using ImageViewer.Utils;
using ImageViewer.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public partial class ExplorerViewModel : ViewModelBase
{
    private readonly ConsoleLogger<ExplorerViewModel> logger = new();
    private readonly IAppState appState;
    private readonly TreeView treeView;

    private FileNode? lastSelectedNode;
    private bool canExpandOrCollapse;
    private Vector lastScrollOffset = Vector.Zero;

    public ExplorerViewModel(ExplorerView parentView)
    {
        appState = ServiceContainer.GetService<IAppState>();
        TreeNodes = appState.FileNodeTree;

        UpdatePathCommand = ReactiveCommand.Create(UpdatePath);

        treeView = parentView.FindControl<TreeView>("FolderTree")!;
        treeView.ContainerPrepared += OnTreeViewContainerPrepared;
        treeView.SelectionChanged += OnTreeViewSelectionChanged;
        treeView.AutoScrollToSelectedItem = false;

        Handlers.RegisterPropertyChangeHandler(appState, new()
        {
            {
                nameof(appState.FileNodeTree),
                () => TreeNodes = appState.FileNodeTree
            },
            {
                nameof(appState.SelectedFolder),
                () =>
                {
                    lastSelectedNode = null;
                    CurrentPath = appState.SelectedFolder?.Path.PathString ?? string.Empty;
                }
            },
        });
    }

    public ReactiveCommand<Unit, Unit> UpdatePathCommand { get; }

    public ObservableCollection<FileNode> TreeNodes
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            canExpandOrCollapse = true;

            if (lastScrollOffset != Vector.Zero)
            {
                treeView.GetVisualDescendants()
                    .OfType<ScrollViewer>()
                    .FirstOrDefault()
                    ?.Offset = lastScrollOffset;
            }
        }
    }

    public string CurrentPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    private void OnTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1 && e.AddedItems[0] is FileNode node)
        {
            logger.Log($"User selected folder: [{node.Resource.Path.PathString}]");
            lastSelectedNode = node;

            if (!(appState.SelectedFolder?.Path.Equals(node.Resource.Path) ?? false))
            {
                appState.SelectedFolder = node.Resource;
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Every time the node collection of the tree view is updated (this occurrs whenever the
    /// user expands or collapses a node), this method will be invoked. This will
    /// ensure that when the tree view is refreshed the node the user had previously selected
    /// will be re-selected.
    /// </summary>
    private void OnTreeViewContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        TreeViewItem treeViewItem = (TreeViewItem)e.Container;
        if (treeViewItem?.DataContext is FileNode treeViewModel)
        {
            treeViewItem.PropertyChanged += OnTreePropertyChanged;
            treeViewItem.ContainerPrepared += OnTreeViewContainerPrepared;

            if (IsLastSelectedNode(treeViewModel)
                || IsNodeMatchingCurrentPath(treeViewModel))
            {
                if (lastScrollOffset == Vector.Zero)
                {
                    treeView.AutoScrollToSelectedItem = true;
                    treeView.SelectedItem = treeViewModel;
                    treeView.AutoScrollToSelectedItem = false;
                }
                else
                {
                    treeView.SelectedItem = treeViewModel;
                }
            }
        }
    }

    private bool IsLastSelectedNode(FileNode treeViewModel)
        => lastSelectedNode != null && treeViewModel.Resource.Path.Equals(lastSelectedNode.Resource.Path);

    private bool IsNodeMatchingCurrentPath(FileNode treeViewModel)
        => CurrentPath != string.Empty && treeViewModel.Resource.Path.PathString == CurrentPath;

    /// <summary>
    /// This method will be invoked whenever a property of a tree node is updated
    /// and will specifically handle what occurrs when the IsExpanded property
    /// changes.
    /// </summary>
    private void OnTreePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not TreeViewItem item
            || e.Property.Name != nameof(TreeViewItem.IsExpanded))
        {
            return;
        }

        if (!canExpandOrCollapse)
        {
            return;
        }

        canExpandOrCollapse = false;

        lastScrollOffset = treeView.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault()
            ?.Offset ?? Vector.Zero;

        FileNode node = (item.DataContext as FileNode)!;
        lastSelectedNode = null;
        if (node.IsExpanded)
        {
            logger.Log($"User collapsed node: [{node.Resource.Path.PathString}]");
            appState.CollapseNode(node);
        }
        else if (node.ContainsOnlyLoadMorePlaceholder())
        {
            logger.Log($"User expanded node: [{node.Resource.Path.PathString}]");
            appState.ExpandNode(node);
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
                "The specified path could not be found. Please check that the path points to a directoy and try again.",
                ButtonEnum.Ok)
                .ShowAsync();
            return;
        }

        lastScrollOffset = Vector.Zero;
        appState.LoadStartingPath(target);
    }
}
