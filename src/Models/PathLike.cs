namespace ImageViewer.Models;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// A wrapper for a Windows path string to ensure the string is normalized
/// and easily comparable to other paths.
/// </summary>
/// <param name="path">The input path string. If this is null the instance
/// will be initialized with an empty string.</param>
public sealed class PathLike(string? path) : IComparable<PathLike>
{
    /// <summary>
    /// Gets the input that string that has been normalized or empty if
    /// no path was provided. The normalized path guarantees that all
    /// folders/files will be separated with a backslash only, and there will be
    /// no trailing slash at the end of a folder name.
    /// </summary>
    public string PathString { get; } = Normalize(path);

    /// <summary>
    /// Creats a new <see cref="PathLike"/> with the <see cref="PathString"/>
    /// set to an empty string.
    /// </summary>
    /// <returns>A new <see cref="PathLike"/> instance with an empty string value.</returns>
    public static PathLike Empty() => new(null);

    /// <summary>
    /// Uses <see cref="File.Exists(string?)"/> to check if the <see cref="PathString"/>
    /// points to a file on disk.
    /// </summary>
    /// <returns>True if <see cref="PathString"/> points to a file on disk.</returns>
    public bool IsFile() => File.Exists(PathString);

    /// <summary>
    /// Uses <see cref="Directory.Exists(string?)"/> to check if the <see cref="PathString"/>
    /// points to a directory on disk.
    /// </summary>
    /// <returns>True if <see cref="PathString"/> points to a directory on disk.</returns>
    public bool IsDirectory() => Directory.Exists(PathString);

    /// <summary>
    /// Uses <see cref="IsFile"/> and <see cref="IsDirectory"/> to determine
    /// if <see cref="PathString"/> points to something on disk.
    /// </summary>
    /// <returns>True if <see cref="PathString"/> points to something on disk.</returns>
    public bool Exists() => IsFile() || IsDirectory();

    /// <summary>
    /// Uses <see cref="Path.GetExtension(string?)"/> to get the extension of
    /// <see cref="PathString"/>.
    /// </summary>
    /// <returns>The extension of the <see cref="PathString"/> if the path has an extension.
    /// If there is no extension on the path this will return null.</returns>
    public string? GetExtension() => Path.GetExtension(PathString);

    /// <summary>
    /// Deleted the <see cref="PathString"/> from disk. If the path string doesn't exist
    /// on disk this will do nothing. If the path is a directory the directory will be
    /// recursively deleted.
    /// </summary>
    public void Delete()
    {
        if (!Exists())
        {
            return;
        }

        if (IsFile())
        {
            File.Delete(PathString);
            return;
        }

        Directory.Delete(PathString, true);
    }

    /// <summary>
    /// Gets the name of the file or directory represented by the <see cref="PathString"/>.
    /// </summary>
    /// <returns>The name of the file or directory represented by the <see cref="PathString"/>.</returns>
    public string GetName()
    {
        if (!Exists())
        {
            return PathString.Split("\\")[^1];
        }

        if (IsDirectory())
        {
            return new DirectoryInfo(PathString).Name;
        }

        return new FileInfo(PathString).Name;
    }

    /// <summary>
    /// Gets an array of <see cref="PathLike"/> directory objects that are direct
    /// children of this object. If this instance is not a directory an empty array will
    /// be returned.
    /// </summary>
    /// <returns>An array of immediate child directories or an empty array if this is
    /// not a directory.</returns>
    public PathLike[] GetChildDirectories()
    {
        if (!IsDirectory())
        {
            return [];
        }

        return [.. Directory.GetDirectories(PathString).Select(dir => new PathLike(dir))];
    }

    /// <summary>
    /// Gets an array of <see cref="PathLike"/> file objects that are direct children
    /// of this object. If this instance is not a directory an empty array will be returned.
    /// </summary>
    /// <returns>An array of immediate child files or an empty array if this path is not a
    /// directory.</returns>
    public PathLike[] GetChildFiles()
    {
        if (!IsDirectory())
        {
            return [];
        }

        return [.. Directory.GetFiles(PathString).Select(file => new PathLike(file))];
    }

    /// <summary>
    /// Uses <see cref="Path.GetDirectoryName(string?)"/> to get the path of the
    /// parent directory that is then piped into a new <see cref="PathLike"/> instance.
    /// </summary>
    /// <returns>A <see cref="PathLike"/> representation of the parent directory.</returns>
    public PathLike GetParentDirectory() => new(Path.GetDirectoryName(PathString));

    /// <summary>
    /// Determines if this <see cref="PathLike"/> is a parent of the input
    /// <see cref="PathLike"/>. This will continually check the parent of the input
    /// path until one of the parents up the file tree either matches this or once
    /// the root of the drive has been reached. If a match is found this will return true.
    /// If the root of the drive has been reached and no match yet found then this will
    /// return false.
    /// </summary>
    /// <param name="other">The path to check against.</param>
    /// <param name="directParentOnly">If true this will only check if the first
    /// parent of the input path is equal to this.</param>
    /// <returns>True if this is a parent directory of the input path.</returns>
    public bool IsParentOf(PathLike other, bool directParentOnly = false)
    {
        if (PathString == string.Empty || other.PathString == string.Empty)
        {
            return false;
        }

        if (PointsTo(other))
        {
            return false;
        }

        PathLike otherParent = other.GetParentDirectory();
        if (directParentOnly && !PointsTo(otherParent))
        {
            return false;
        }

        while (otherParent.IsDirectory())
        {
            if (PointsTo(otherParent))
            {
                return true;
            }

            otherParent = otherParent.GetParentDirectory();
        }

        return false;
    }

    /// <summary>
    /// Compares this instance to another object. This is considered equal to the other object
    /// if the other object is a <see cref="PathLike"/> and the two are equal based
    /// on the criteria in the <see cref="PointsTo(PathLike)"/> method.
    /// </summary>
    /// <param name="obj">The other object to compare this one to.</param>
    /// <returns>True if the aforementioned criteria has been met.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not PathLike pathLike)
        {
            return false;
        }

        return PointsTo(pathLike);
    }

    /// <summary>
    /// Gets the hashcode.
    /// </summary>
    /// <returns>The hashcode.</returns>
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>
    /// Gets the string representation of this pathlike object which is simply
    /// value of the <see cref="PathString"/>.
    /// </summary>
    /// <returns>The value of the <see cref="PathString"/>.</returns>
    public override string ToString() => PathString;

    /// <summary>
    /// Compares this to another <see cref="PathLike"/> instance. This
    /// effectively compares the two by comparing the value of the
    /// <see cref="PathString"/> property.
    /// </summary>
    /// <param name="other">The other <see cref="PathLike"/> instance to compare to.</param>
    /// <returns>The result of <see cref="PathString"/>'s <see cref="string.CompareTo(string?)"/> method.</returns>
    public int CompareTo(PathLike? other) => string.Compare(PathString, other?.PathString, StringComparison.Ordinal);

    private static string Normalize(string? path)
    {
        if (path == null)
        {
            return string.Empty;
        }

        return Path.GetFullPath(path).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private bool PointsTo(PathLike other)
    {
        if (other.PathString == string.Empty || PathString == string.Empty)
        {
            return false;
        }

        return PathString == other.PathString;
    }
}
