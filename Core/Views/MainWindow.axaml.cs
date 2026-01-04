namespace ImageViewer.Core.Views;

using Avalonia.Controls;
using ImageViewer.Core.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow(string[] arguments)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this, arguments);
    }
}