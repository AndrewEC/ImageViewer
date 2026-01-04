namespace ImageViewer.Core.Views;

using Avalonia.Controls;
using ImageViewer.Core.ViewModels;

public partial class ExplorerView : UserControl
{
    public ExplorerView()
    {
        InitializeComponent();
        DataContext = new ExplorerViewModel(this);
    }
}