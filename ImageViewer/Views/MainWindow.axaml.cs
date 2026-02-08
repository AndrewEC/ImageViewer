namespace ImageViewer.Views;

using Avalonia.Controls;
using ImageViewer.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow(string[] arguments)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this, arguments);
    }
}