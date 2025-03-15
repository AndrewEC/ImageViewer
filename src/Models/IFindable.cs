namespace ImageViewer.Models;

/// <summary>
/// Indicates the underlying type should be "findable" in disk
/// as it will have an absolute path referencing it's location.
/// </summary>
public interface IFindable
{
    /// <summary>
    /// Gets the absolute path to the file on disk.
    /// </summary>
    string AbsolutePath { get; }
}