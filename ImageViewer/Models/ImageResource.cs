namespace ImageViewer.Models;

using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ImageViewer.Injection;
using ImageViewer.State;

public sealed class ImageResource(PathLike path)
{
    public PathLike Path { get; } = path;

    public string Name { get; } = path.Name();

    public Task<Bitmap?> Thumbnail
    {
        get => ServiceContainer.GetService<IImageCache>().LoadThumbnail(Path.PathString);
    }

    public Task<Bitmap?> Image
    {
        get => ServiceContainer.GetService<IImageCache>().LoadImage(Path.PathString);
    }
}