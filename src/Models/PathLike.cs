namespace ImageViewer.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class PathLike
{
    public PathLike(string path)
    {
        PathString = Normalize(path);
    }

    public PathLike(PathLike source)
    : this(source.PathString) { }

    public string PathString { get; }

    public bool IsFile() => File.Exists(PathString);

    public bool IsDirectory() => Directory.Exists(PathString);

    public bool Exists() => IsFile() || IsDirectory();

    public string GetName() => GetPathSegments()[^1];

    public string GetStem()
    {
        string fileName = GetPathSegments()[^1];
        if (IsDirectory() || !Exists())
        {
            return fileName;
        }

        int index = fileName.IndexOf('.');
        if (index == -1)
        {
            return fileName;
        }

        return fileName[..index];
    }

    public bool Delete()
    {
        if (!Exists())
        {
            return false;
        }

        try
        {
            if (IsDirectory())
            {
                Directory.Delete(PathString, true);
            }
            else
            {
                File.Delete(PathString);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string GetExtension()
    {
        if (IsDirectory())
        {
            return string.Empty;
        }

        return Path.GetExtension(PathString);
    }

    public PathLike GetParentDirectory()
    {
        string[] segments = GetPathSegments();
        if (segments.Length == 0 || segments.Length == 1)
        {
            return this;
        }

        string parentPath = string
            .Join(Path.DirectorySeparatorChar, segments[0..(segments.Length - 1)]);

        return new(parentPath);
    }

    public string[] GetPathSegments() => PathString.Split(Path.DirectorySeparatorChar)
        .Where(segment => segment != string.Empty)
        .ToArray();

    public PathLike GetRoot() => new(GetPathSegments()[0]);

    public PathLike Join(string segment) => new(PathString + Path.DirectorySeparatorChar + segment);

    public List<PathLike> GetChildFiles()
    {
        if (!IsDirectory())
        {
            return [];
        }

        try
        {
            return Directory.GetFiles(PathString + Path.DirectorySeparatorChar)
                .Select(file => new PathLike(file))
                .ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public List<PathLike> GetChildDirectories()
    {
        if (!IsDirectory())
        {
            return [];
        }

        try
        {
            return Directory.GetDirectories(PathString + Path.DirectorySeparatorChar)
                .Select(directory => new PathLike(directory))
                .ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public override int GetHashCode() => PathString.GetHashCode();

    public override bool Equals(object? obj)
        => obj is PathLike other && other.PathString == PathString;

    public override string ToString() => PathString;

    private static string Normalize(string path)
        => path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);
}