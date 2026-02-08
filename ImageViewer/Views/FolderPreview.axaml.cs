namespace ImageViewer.Views;

using Avalonia.Controls;
using ImageViewer.ViewModels;

public partial class FolderPreview : UserControl
{
    public FolderPreview()
    {
        InitializeComponent();
        DataContext = new FolderPreviewViewModel();
    }
}