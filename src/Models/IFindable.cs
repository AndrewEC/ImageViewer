namespace ImageViewer.Models;

/// <summary>
/// Indicates the underlying type should be "findable" in disk
/// as it will have an absolute path referencing it's location.
/// </summary>
public interface IPathResource
{
    /// <summary>
    /// Gets a representation of the path to the file on disk.
    /// </summary>
    PathLike Path { get; }
}