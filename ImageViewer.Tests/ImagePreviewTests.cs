namespace ImageViewer.Tests;

using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using ImageViewer.Models;
using ImageViewer.Views;

#pragma warning disable CA1707
[TestFixture]
public class ImagePreviewTests
{
    [SetUp]
    public void SetUp()
    {
        AppState.Instance.SelectedFolder = null;
        AppState.Instance.SelectedTabIndex = (int)Tabs.ImagePreview;
    }

    [AvaloniaTest]
    public async Task ImagePreview_ShouldShowImage_WhenLaunchedWithImagePath()
    {
        PathLike imagePath = TestData.TestImagePath.Join(TestData.TestImages[0]);

        var window = new MainWindow([imagePath.PathString]);
        window.Show();

        Assert.That(AppState.Instance.SelectedTabIndex, Is.EqualTo((int)Tabs.ImagePreview));

        for (int i = 0; i < TestData.TestImages.Length; i++)
        {
            await Task.Delay(100);

            Assert.That(AppState.Instance.SelectedImage?.Name, Is.EqualTo(TestData.TestImages[i]));

            window.KeyPressQwerty(PhysicalKey.D, RawInputModifiers.None);
        }
    }
}
#pragma warning restore CA1707