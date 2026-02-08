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

    public string Name() => Segments()[^1];

    public string Stem()
    {
        string fileName = Segments()[^1];
        if (IsDirectory() || !Exists())
        {
            return fileName;
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    public bool CreateDirectory()
    {
        if (IsDirectory())
        {
            return true;
        }

        try
        {
            Directory.CreateDirectory(PathString);
            return true;
        }
        catch
        {
            return false;
        }
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

    public string Extension()
    {
        if (IsDirectory())
        {
            return string.Empty;
        }

        return Path.GetExtension(PathString);
    }

    public PathLike Parent()
    {
        string[] segments = Segments();
        if (segments.Length == 0 || segments.Length == 1)
        {
            return this;
        }

        string parentPath = string
            .Join(Path.DirectorySeparatorChar, segments[0..(segments.Length - 1)]);

        return new(parentPath);
    }

    public string[] Segments() => PathString.Split(Path.DirectorySeparatorChar)
        .Where(segment => segment != string.Empty)
        .ToArray();

    public PathLike Root() => new(Segments()[0]);

    public PathLike Join(string segment) => new(Path.Join(PathString, segment));

    public List<PathLike> ChildFiles()
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

    public List<PathLike> ChildDirectories()
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