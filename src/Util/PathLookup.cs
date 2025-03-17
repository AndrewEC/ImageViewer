namespace ImageViewer.Util;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ImageViewer.Models;

/// <summary>
/// A static utility class to help lookup folders and images in
/// specified input paths.
/// </summary>
public static class PathLookup
{
    /// <summary>
    /// The array of supported image file extensions.
    /// </summary>
    public static readonly ImmutableArray<string> SupportedImageExtensions = [
        ".png",
        ".jpg",
        ".webp",
        ".jpeg"
    ];

    /// <summary>
    /// Returns an array representing all the absolute paths to the image
    /// files with a supported format that are directly under the input directory.
    /// This will not recursively scan the folder. It will only scan files
    /// directly within. If the directory does not exist this will return an
    /// empty array.
    /// </summary>
    /// <param name="directory">The directory to scan for images within.</param>
    /// <returns>An array representing the absolute paths to the images found.</returns>
    public static PathLike[] GetSupportedImagesInFolder(PathLike directory) => directory.GetChildFiles()
            .Where(file => SupportedImageExtensions.Contains(file.GetExtension() ?? string.Empty))
            .ToArray();

    /// <summary>
    /// Gets an array of all the folders that exist within the input path. If the input
    /// path does not exist or is not a directory then this will return an empty array.
    /// </summary>
    /// <param name="path">The absolute path to the root folder to be scanned.</param>
    /// <returns>An array of all the folders that could be found within the specified path.</returns>
    public static FolderItem[] RecursivelyFindSubFolders(PathLike path)
        => [.. YieldSubFolders(path).Select(MapToFolderItem)];

    private static IEnumerable<PathLike> YieldSubFolders(PathLike path)
    {
        yield return path;
        foreach (PathLike directory in path.GetChildDirectories())
        {
            foreach (PathLike subDir in YieldSubFolders(directory))
            {
                yield return subDir;
            }
        }
    }

    private static FolderItem MapToFolderItem(PathLike directory)
    {
        PathLike? coverImage = GetSupportedImagesInFolder(directory).FirstOrDefault();
        return new FolderItem(directory, directory.GetName(), coverImage);
    }
}