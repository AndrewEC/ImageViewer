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
public class SettingsViewTests
{
    private static readonly string SaveButtonName = "SettingsViewSaveButton";
    private static readonly string ScanDepthComboBoxName = "SettingsViewScanDepth";
    private static readonly string ResetButtonName = "SettingsViewResetButton";

    [SetUp]
    public void SetUp()
    {
        AppState.Instance.SelectedFolder = null;
        AppState.Instance.SelectedTabIndex = (int)Tabs.ImagePreview;
    }

    [AvaloniaTest]
    public async Task SettingsView_ShouldAllowSaving_WhenUserChangesSettings()
    {
        var window = new MainWindow([]);
        window.Show();

        AppState.Instance.SelectedTabIndex = (int)Tabs.Settings;
        await Task.Delay(100);

        Button saveButton = Utils.FindNestedControl<SettingsView, Button>(window, SaveButtonName);
        Button resetButton = Utils.FindNestedControl<SettingsView, Button>(window, ResetButtonName);
        ComboBox scanDepthComboBox = Utils.FindNestedControl<SettingsView, ComboBox>(window, ScanDepthComboBoxName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(saveButton.IsEnabled, Is.False);
            Assert.That(resetButton.IsEnabled, Is.False);
        }

        scanDepthComboBox.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(saveButton.IsEnabled, Is.True);
            Assert.That(resetButton.IsEnabled, Is.True);
        }

        resetButton.Focus();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(saveButton.IsEnabled, Is.False);
            Assert.That(resetButton.IsEnabled, Is.False);
        }
    }
}
#pragma warning restore CA1707