namespace ImageViewer.Core.Utils;

using Avalonia.Controls;

public static class GridUtil
{
    
    public static void ResizeColumns(Grid grid, int[] columnWidths)
    {
        for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
        {
            grid.ColumnDefinitions[i].Width = new GridLength(columnWidths[i], GridUnitType.Star);
        }
    }

    public static void ResizeRows(Grid grid, int[] rowHeights)
    {
        for (int i = 0; i < grid.RowDefinitions.Count; i++)
        {
            grid.RowDefinitions[i].Height = new GridLength(rowHeights[i], GridUnitType.Star);
        }
    }

}