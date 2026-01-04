namespace ImageViewer.Core.Views;

using Avalonia.Controls;
using ImageViewer.Core.ViewModels;

public partial class FolderPreview : UserControl
{
    public FolderPreview()
    {
        InitializeComponent();
        DataContext = new FolderPreviewViewModel();
    }
}