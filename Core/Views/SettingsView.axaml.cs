namespace ImageViewer.Core.Views;

using Avalonia.Controls;
using ImageViewer.Core.ViewModels;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}