namespace ImageViewer.Utils;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageViewer.Config;
using ImageViewer.Models;

public static class Scan
{
    private static readonly List<string> SupportedImageExtensions = [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".webp"
    ];

    public static List<ImageResource> GetImageResourcesInFolder(FileResource? resource)
    {
        if (resource == null)
        {
            return [];
        }

        Config config = ConfigState.Instance.LoadConfig();
        int scanDepth = config.ScanDepth;
        IComparer<PathLike> comparer = Sorting.GetComparer(config.SortMethod);

        return GetFoldersToScan(resource.Path, scanDepth)
            .SelectMany(folder => folder.GetChildFiles())
            .Where(file => IsPotentiallyImageFile(file))
            .OrderBy(file => file, comparer)
            .Select(file => new ImageResource(file))
            .ToList();
    }

    public static bool IsPotentiallyImageFile(PathLike path) => SupportedImageExtensions.Contains(path.GetExtension());

    private static List<PathLike> GetFoldersToScan(PathLike root, int scanDepth)
    {
        List<List<PathLike>> folders = [[root]];
        for (int i = 1; i < scanDepth; i++)
        {
            List<PathLike> pathsToScan = folders[i - 1];
            if (pathsToScan.Count == 0)
            {
                break;
            }

            List<PathLike> foundFolders = [.. pathsToScan.SelectMany(folder => folder.GetChildDirectories())];

            folders.Add(foundFolders);
        }

        return [.. folders.SelectMany(folder => folder)];
    }

    public static List<FileResource> GetDriveRootResources()
        => DriveInfo.GetDrives()
            .Select(drive => new PathLike(drive.Name))
            .Select(path => new FileResource(path, true))
            .ToList();

    /// <summary>
    /// Attempts to "expand" all the directories specified in the startingPath.
    /// <para>
    /// To "expand" a directory means to identify, and keep a reference to, all the
    /// directories that are direct children of the directory being expanded. (Direct meaning
    /// no recursive searches for nested children directories.)
    /// </para>
    /// <para>
    /// For example given the path "C:\albums\nature" this method will first look for the root
    /// drive C:\. Then it will "expand" the C:\ directory by looking up all the directories
    /// directly within C:\ and keeping a reference to them. Then it will look for a directory
    /// within C:\ called "albums" and repeat the process for both "albums" and for the "nature"
    /// directories.
    /// </para>
    /// </summary>
    /// <param name="startingPath"></param>
    /// <param name="rootResource"></param>
    public static void ExpandPath(PathLike startingPath, List<FileResource> rootResources)
    {
        FileResource? rootResource = FindMatchingRoot(startingPath, rootResources);
        if (rootResource == null)
        {
            return;
        }

        rootResource.Expand();

        FileResource next = rootResource;
        string[] segments = startingPath.GetPathSegments();
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
        PathLike startingRoot = startingPath.GetRoot();
        return rootResources.Find(resource => resource.Path.Equals(startingRoot));
    }

    public static FileResource? FindFileResourceMatchingPath(PathLike desiredPath, List<FileResource> rootResources)
    {
        FileResource? rootResource = FindMatchingRoot(desiredPath, rootResources);
        if (rootResource == null)
        {
            return null;
        }

        FileResource next = rootResource;
        string[] segments = desiredPath.GetPathSegments();
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