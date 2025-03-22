namespace ImageViewer.Util;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ImageViewer.Models;

/// <summary>
/// A static utility class to help lookup folders and images in
/// specified input paths.
/// </summary>
public static class PathLikeExtensions
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
    /// Returns a subset of the source enumerable such that each resulting element in the
    /// enumerable has a <see cref="IPathResource.Path"/> equal to the input path.
    /// </summary>
    /// <typeparam name="T">The enumerable collection type. Must be of type <see cref="IPathResource"/>.</typeparam>
    /// <param name="source">The collection to filter on.</param>
    /// <param name="pathToFind">The path to match against each <see cref="IPathResource.Path"/> of
    /// each element in the input source enumerable.</param>
    /// <returns>An enumerable collection in which each element in the collection has an AbsolutePath
    /// matching the input path.</returns>
    public static IEnumerable<T> WhereByPath<T>(this IEnumerable<T> source, PathLike? pathToFind)
    where T : IPathResource => source.Where(s => s.Path.Equals(pathToFind));

    /// <summary>
    /// Returns the first element in the enumerable in which the element has a
    /// <see cref="IPathResource.Path"/> matching the input path.
    /// </summary>
    /// <typeparam name="T">The enumerable collection type. Must be of type <see cref="IPathResource"/>.</typeparam>
    /// <param name="source">The collection to filter on.</param>
    /// <param name="pathToFind">The path to match against each <see cref="IPathResource.Path"/> of
    /// each element in the input source enumerable.</param>
    /// <returns>The first element with a matching path or null.</returns>
    public static T? FirstByPath<T>(this IEnumerable<T> source, PathLike? pathToFind)
    where T : IPathResource => source.WhereByPath(pathToFind).FirstOrDefault();

    /// <summary>
    /// Enumerates all image files within the input directory that have one of the supported
    /// image extensions. If the input directory does not exist, or is not a directory, the
    /// resulting enumerable will be empty.
    /// </summary>
    /// <param name="directory">The directory to scan for images within.</param>
    /// <returns>An array representing the absolute paths to the images found.</returns>
    public static IEnumerable<ImageResource> EnumerateChildImageResources(PathLike? directory)
        => (directory?.EnumerateChildFiles() ?? [])
            .Where(file => SupportedImageExtensions.Contains(file.GetExtension() ?? string.Empty))
            .Select(file => new ImageResource(file));

    /// <summary>
    /// Enumerates all the rerusrively found folders that exist within the input path.
    /// If the input path does not exist or is not a directory the resulting enumerable
    /// will be empty.
    /// </summary>
    /// <param name="directory">The absolute path to the root folder to be scanned.</param>
    /// <returns>An array of all the folders that could be found within the specified path.</returns>
    public static IEnumerable<FolderResource> EnumerateChildDirectoryResources(PathLike? directory)
        => (directory?.EnumerateChildDirectories(true) ?? []).Select(ToFolderResource);

    public static FolderResource ToFolderResource(PathLike directory)
    {
        ImageResource? coverImage = EnumerateChildImageResources(directory).FirstOrDefault();
        return new FolderResource(directory, directory.GetName(), coverImage?.Path ?? PathLike.Empty());
    }
}