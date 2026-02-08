namespace ImageViewer.Utils;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImageViewer.Models;

public interface ISorting
{
    IComparer<PathLike> GetComparer(SortMethod sortMethod);
}

public class Sorting : ISorting
{
    private static readonly Dictionary<SortMethod, IComparer<PathLike>> Comparers = new()
    {
        { SortMethod.Natural, new NaturalComparer() },
        { SortMethod.WindowsLike, new WindowsLikeComparer() },
    };

    public IComparer<PathLike> GetComparer(SortMethod sortMethod)
        => Comparers[sortMethod];

    /// <summary>
    /// "Windows Like" sorting attempts to parse the file names to a number
    /// then compare the resulting numbers to determine the order. If either file
    /// name cannot be parsed to an integer this will fallback to comparing the
    /// string values for order.
    /// <para>
    /// This is to match the default Windows behaviour.
    /// </para>
    /// </summary>
    public class WindowsLikeComparer : IComparer<PathLike>
    {
        private static readonly int RangeMin = 48;
        private static readonly int RangeMax = 57;
        private static readonly int NumbersInInt32MaxValue = 10;

        private readonly NaturalComparer naturalComparer = new();

        public int Compare(PathLike? x, PathLike? y)
        {
            int? x1 = TryParseInt(x?.Stem());
            if (x1 == null)
            {
                return naturalComparer.Compare(x, y);
            }

            int? y1 = TryParseInt(y?.Stem());
            if (y1 == null)
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
            => string.CompareOrdinal(x?.Stem(), y?.Stem());
    }
}