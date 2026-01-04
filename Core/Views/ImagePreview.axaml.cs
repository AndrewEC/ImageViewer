namespace ImageViewer.Core.Views;

using Avalonia.Controls;
using ImageViewer.Core.ViewModels;

public partial class ImagePreview : UserControl
{
    public ImagePreview()
    {
        InitializeComponent();
        DataContext = new ImagePreviewViewModel(this);
    }
}