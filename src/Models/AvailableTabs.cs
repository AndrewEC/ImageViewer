namespace ImageViewer.Models;

/// <summary>
/// Numeric enumeration representing the indexes of the tabs
/// available in the main tabs control.
/// </summary>
public enum AvailableTabs
{
    /// <summary>
    /// The first tab where the root folder can be selected and the
    /// list of nested folders viewed.
    /// </summary>
    FolderList = 0,

    /// <summary>
    /// The second tab where the images of a specific folder can
    /// be previewed.
    /// </summary>
    FolderPreview = 1,

    /// <summary>
    /// The third tab where a specific image can be viewed in
    /// larger dimensions.
    /// </summary>
    ImagePreview = 2,
}