namespace ImageViewer.Models;

using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ImageViewer.State;

public sealed class ImageResource(PathLike path)
{
    public PathLike Path { get; } = path;

    public string Name { get; } = path.Name();

    public Task<Bitmap?> Thumbnail
    {
        get => ImageCache.Instance.LoadThumbnail(Path.PathString);
    }

    public Task<Bitmap?> Image
    {
        get => ImageCache.Instance.LoadImage(Path.PathString);
    }
}