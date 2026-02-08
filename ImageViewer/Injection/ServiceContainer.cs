namespace ImageViewer.Injection;

using ImageViewer.State;
using ImageViewer.Utils;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceContainer
{
    public static readonly ServiceContainer Instance = new();

    private readonly ServiceProvider provider;

    private ServiceContainer()
    {
        ServiceCollection services = new();

        services.AddSingleton<IImageCache, ImageCache>();
        services.AddSingleton<IConfigState, ConfigState>();
        services.AddSingleton<ISorting, Sorting>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IScan, Scan>();
        services.AddSingleton<IInitializer, Initializer>();

        provider = services.BuildServiceProvider();
    }

    private T DoGetService<T>() => provider.GetService<T>()!;

    public static T GetService<T>() => Instance.DoGetService<T>();
}