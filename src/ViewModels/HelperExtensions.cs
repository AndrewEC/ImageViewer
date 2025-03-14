namespace ImageViewer.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Common extensions to simplify some repeated code.
/// </summary>
public static class HelperExtensions
{
    /// <summary>
    /// Determines if the index falls within the bounds of the array.
    /// </summary>
    /// <typeparam name="T">The type of the underlying enumerable.</typeparam>
    /// <param name="source">The underlying enumerable to check against.</param>
    /// <param name="index">The index to be verified.</param>
    /// <returns>True if the index falls within the bounds of the array otherwise, false.</returns>
    public static bool IsValidIndex<T>(this IEnumerable<T> source, int index) => index >= 0 && index < source.Count();
}
