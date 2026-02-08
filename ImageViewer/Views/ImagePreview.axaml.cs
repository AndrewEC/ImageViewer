namespace ImageViewer.Views;

using Avalonia.Controls;
using ImageViewer.ViewModels;

public partial class ImagePreview : UserControl
{
    public ImagePreview()
    {
        InitializeComponent();
        DataContext = new ImagePreviewViewModel(this);
    }
}