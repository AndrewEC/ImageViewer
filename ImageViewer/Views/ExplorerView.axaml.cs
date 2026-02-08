namespace ImageViewer.Views;

using Avalonia.Controls;
using ImageViewer.ViewModels;

public partial class ExplorerView : UserControl
{
    public ExplorerView()
    {
        InitializeComponent();
        DataContext = new ExplorerViewModel(this);
    }
}