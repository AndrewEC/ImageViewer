using Avalonia;
using Avalonia.Headless;
using ImageViewer.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace ImageViewer.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
