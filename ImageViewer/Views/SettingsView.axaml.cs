namespace ImageViewer.Views;

using Avalonia.Controls;
using ImageViewer.ViewModels;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}