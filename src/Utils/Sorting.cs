namespace ImageViewer.Utils;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImageViewer.Models;

public static class Sorting
{
    private static readonly Dictionary<SortMethod, IComparer<PathLike>> Comparers = new()
    {
        { SortMethod.Natural, new NaturalComparer() },
        { SortMethod.WindowsLike, new WindowsLikeComparer() },
    };

    public static IComparer<PathLike> GetComparer(SortMethod sortMethod)
        => Comparers[sortMethod];

    public class WindowsLikeComparer : IComparer<PathLike>
    {
        private static readonly int RangeMin = 48;
        private static readonly int RangeMax = 57;
        private static readonly int NumbersInInt32MaxValue = 10;

        private readonly NaturalComparer naturalComparer = new();

        public int Compare(PathLike? x, PathLike? y)
        {
            int? x1 = TryParseInt(x?.GetStem());
            int? y1 = TryParseInt(y?.GetStem());
            if (x1 == null || y1 == null)
            {
                return naturalComparer.Compare(x, y);
            }

            return (int)(x1 - y1);
        }

        private static int? TryParseInt(string? fileName)
        {
            if (!IsPossibleInteger(fileName))
            {
                return null;
            }

            try
            {
                return Convert.ToInt32(fileName, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPossibleInteger(string? value)
        {
            if (value == null || value.Length == 0 || value.Length > NumbersInInt32MaxValue)
            {
                return false;
            }

            return value.ToCharArray().All(v => v >= RangeMin && v <= RangeMax);
        }
    }

    public class NaturalComparer : IComparer<PathLike>
    {
        public int Compare(PathLike? x, PathLike? y)
            => string.CompareOrdinal(x?.GetStem(), y?.GetStem());
    }
}