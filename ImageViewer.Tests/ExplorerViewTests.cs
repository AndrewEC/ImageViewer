namespace ImageViewer.Tests;

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using ImageViewer.Models;
using ImageViewer.Views;

#pragma warning disable CA1707
[TestFixture]
public class ExplorerViewTests
{
    private static readonly string ExplorerInputName = "ExplorerPathInput";
    private static readonly string TreeViewName = "FolderTree";

    [SetUp]
    public void SetUp()
    {
        AppState.Instance.SelectedFolder = null;
        AppState.Instance.SelectedTabIndex = (int)Tabs.ImagePreview;
    }

    [AvaloniaTest]
    public void ExplorerView_ShouldShowDefaultPath_WhenLaunchedWithNoArguments()
    {
        var window = new MainWindow([]);
        window.Show();

        TextBox input = Utils.FindNestedControl<ExplorerView, TextBox>(window, ExplorerInputName);
        Assert.That(input.Text, Is.EqualTo(TestData.DownloadPath.PathString));

        AssertTreeViewSelection(window, TestData.DownloadPath);

        Assert.That(AppState.Instance.SelectedTabIndex, Is.EqualTo((int)Tabs.Folders));
    }

    [AvaloniaTest]
    public void ExplorerView_ShouldShowPath_WhenFolderPathIsProvidedAsLaunchArgument()
    {
        var window = new MainWindow([TestData.ExecutingPath.PathString]);
        window.Show();

        TextBox input = Utils.FindNestedControl<ExplorerView, TextBox>(window, ExplorerInputName);
        Assert.That(input.Text, Is.EqualTo(TestData.ExecutingPath.PathString));

        AssertTreeViewSelection(window, TestData.ExecutingPath);
        Assert.That(AppState.Instance.SelectedTabIndex, Is.EqualTo((int)Tabs.Folders));
    }

    [AvaloniaTest]
    public async Task ExplorerView_ShouldShowUpdatedPath_WhenNewPathIsEnteredInInputField()
    {
        var window = new MainWindow([]);
        window.Show();

        TextBox input = Utils.FindNestedControl<ExplorerView, TextBox>(window, ExplorerInputName);
        input.Focus();
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
        window.KeyTextInput(TestData.ExecutingPath.PathString);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        await Task.Delay(100);

        Assert.That(input.Text, Is.EqualTo(TestData.ExecutingPath.PathString));

        AssertTreeViewSelection(window, TestData.ExecutingPath);
    }

    [AvaloniaTest]
    public void ExplorerView_ShouldShowImageThumbnails_WhenLaunchPathContainsImages()
    {
        var window = new MainWindow([TestData.TestImagePath.PathString]);
        window.Show();

        Assert.That(AppState.Instance.SelectedFolderResources, Has.Count.EqualTo(3));
    }

    [AvaloniaTest]
    public async Task ExplorerView_ShouldNavigateToImagePreview_WhenImageIsClicked()
    {
        var window = new MainWindow([TestData.TestImagePath.PathString]);
        window.Show();

        Utils.FindNestedControl<ExplorerView, TextBox>(window, ExplorerInputName).Focus();

        // Tab to the first available button representing an image in the TestImagePath.
        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);

        Assert.That(AppState.Instance.SelectedTabIndex, Is.EqualTo((int)Tabs.Folders));
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        await Task.Delay(100);
        Assert.That(AppState.Instance.SelectedTabIndex, Is.EqualTo((int)Tabs.ImagePreview));
    }

    private static void AssertTreeViewSelection(Window window, PathLike expectedSelection)
    {
        TreeView treeView = Utils.FindNestedControl<ExplorerView, TreeView>(window, TreeViewName);
        FileNode? selected = treeView.SelectedItem as FileNode;
        Assert.That(selected, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(selected.IsExpanded, Is.True);
            Assert.That(selected.Title, Is.EqualTo(expectedSelection.Name()));
            Assert.That(selected.Resource.Path, Is.EqualTo(expectedSelection));
        }

        Assert.That(AppState.Instance.SelectedTabIndex, Is.EqualTo((int)Tabs.Folders));
    }
}
#pragma warning restore CA1707