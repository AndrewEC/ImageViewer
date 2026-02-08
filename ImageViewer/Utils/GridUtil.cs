namespace ImageViewer.Utils;

using System.Collections.Immutable;
using Avalonia.Controls;

public static class GridUtil
{
    
    public static void ResizeColumns(Grid grid,
        ImmutableArray<int> columnWidths,
        GridUnitType gridUnitType=GridUnitType.Star)
    {
        for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
        {
            grid.ColumnDefinitions[i].Width = new GridLength(columnWidths[i], gridUnitType);
        }
    }

    public static void ResizeRows(Grid grid,
        ImmutableArray<int> rowHeights,
        GridUnitType gridUnitType=GridUnitType.Star)
    {
        for (int i = 0; i < grid.RowDefinitions.Count; i++)
        {
            grid.RowDefinitions[i].Height = new GridLength(rowHeights[i], gridUnitType);
        }
    }

}