namespace ImageViewer.Utils;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageViewer.Models;
using ImageViewer.State;

public interface IScan
{
    List<ImageResource> GetImageResources(FileResource? resource);

    bool IsPotentiallyImageFile(PathLike path);

    List<FileResource> GetDriveRootResources();

    void ExpandPath(PathLike startingPath, List<FileResource> rootResources);

    FileResource? FindResourceInTree(PathLike desiredPath, List<FileResource> rootResources);
}

public class Scan(IConfigState configState, ISorting sorting) : IScan
{
    private static readonly List<string> SupportedImageExtensions = [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".webp"
    ];

    private readonly IConfigState configState = configState;
    private readonly ISorting sorting = sorting;

    public List<ImageResource> GetImageResources(FileResource? resource)
    {
        if (resource == null)
        {
            return [];
        }

        Config config = configState.LoadConfig();
        int scanDepth = config.ScanDepth;
        IComparer<PathLike> comparer = sorting.GetComparer(config.SortMethod);

        return GetFoldersToScan(resource.Path, scanDepth)
            .SelectMany(folder => folder.ChildFiles())
            .Where(IsPotentiallyImageFile)
            .OrderBy(file => file, comparer)
            .Select(file => new ImageResource(file))
            .ToList();
    }

    public bool IsPotentiallyImageFile(PathLike path) => SupportedImageExtensions.Contains(path.Extension());

    private static List<PathLike> GetFoldersToScan(PathLike root, int scanDepth)
    {
        List<List<PathLike>> folders = [[root]];
        for (int i = 0; i < scanDepth - 1; i++)
        {
            List<PathLike> pathsToScan = folders[i];
            if (pathsToScan.Count == 0)
            {
                break;
            }

            List<PathLike> foundFolders = [.. pathsToScan.SelectMany(folder => folder.ChildDirectories())];

            folders.Add(foundFolders);
        }

        return [.. folders.SelectMany(folder => folder)];
    }

    public List<FileResource> GetDriveRootResources()
        => DriveInfo.GetDrives()
            .Select(drive => new PathLike(drive.Name))
            .Select(path => new FileResource(path, true))
            .ToList();

    /// <summary>
    /// Attempts to "expand" all the directories specified in the startingPath.
    /// <para>
    /// For example given the path "C:\albums\nature" this method will first look for the root
    /// drive C:\ within the list rootResources. Then it will "expand" the C:\ directory by looking
    /// up all the directories directly within C:\ and keeping a reference to them. It may
    /// find folders like "albums", "songs" and "videos". Then it will take the "albums" folder
    /// "C:\albums" and identify all of its immediate children. It may find the folders "nature"
    /// and "urban". It will then repeat this process with the "nature" folder.
    /// </para>
    /// </summary>
    public void ExpandPath(PathLike startingPath, List<FileResource> rootResources)
    {
        FileResource? rootResource = FindMatchingRoot(startingPath, rootResources);
        if (rootResource == null)
        {
            return;
        }

        rootResource.Expand();

        FileResource next = rootResource;
        string[] segments = startingPath.Segments();
        for (int i = 1; i < segments.Length; i++)
        {
            FileResource? child = next.GetChildWithName(segments[i]);
            if (child == null)
            {
                break;
            }

            child.Expand();

            next = child;
        }
    }

    private static FileResource? FindMatchingRoot(PathLike startingPath, List<FileResource> rootResources)
    {
        PathLike startingRoot = startingPath.Root();
        return rootResources.Find(resource => resource.Path.Equals(startingRoot));
    }

    public FileResource? FindResourceInTree(PathLike desiredPath, List<FileResource> rootResources)
    {
        FileResource? rootResource = FindMatchingRoot(desiredPath, rootResources);
        if (rootResource == null)
        {
            return null;
        }

        FileResource next = rootResource;
        string[] segments = desiredPath.Segments();
        for (int i = 1; i < segments.Length; i++)
        {
            FileResource? child = next.GetChildWithName(segments[i]);
            if (child == null)
            {
                return null;
            }

            next = child;
        }

        return next;
    }
}