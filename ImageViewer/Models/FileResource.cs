namespace ImageViewer.Models;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public sealed class FileResource
{
    public FileResource(PathLike path, bool createPlaceholderValueForChildren)
    {
        Path = path;

        if (createPlaceholderValueForChildren)
        {
            Children = [CreateChildPlaceholder()];
        }
    }

    public PathLike Path { get; }

    public List<FileResource> Children { get; private set; } = [];

    public void Expand()
    {
        Children = [.. Path.ChildDirectories().Select(child => new FileResource(child, true))];
    }

    public void Collapse() => Children = [CreateChildPlaceholder()];

    public FileResource? GetChildWithName(string name)
    {
        if (Children.Count == 0)
        {
            return null;
        }

        return Children.Find(child => child.Path.Name() == name);
    }

    public FileNode ToNodeTree()
    {
        ObservableCollection<FileNode> subNodes = [.. Children.Select(child => child.ToNodeTree())];
        return new(Path.Name(), this, subNodes);
    }

    private FileResource CreateChildPlaceholder() => new(Path.Join(FileNode.LoadingPlaceholderTitle), false);
}