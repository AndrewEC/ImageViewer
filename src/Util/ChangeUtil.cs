namespace ImageViewer.Util;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Common extensions to simplify some repeated code.
/// </summary>
public static class ChangeUtil
{
    /// <summary>
    /// Checks to see if two values might be the same. If both values are
    /// <see cref="Array"/>s then this will perform a shallow comparison
    /// between each of the elements in the array. If at least one of the value is
    /// not an array a comparison will be made using <see cref="object.Equals(object?, object?)"/>.
    /// </summary>
    /// <param name="currentValue">The first value to compare.</param>
    /// <param name="nextValue">The second value to compare.</param>
    /// <returns>True if the objects are equal based on the aforemetioned criteria.</returns>
    public static bool AreRoughlyEqual(object? currentValue, object? nextValue)
    {
        if (currentValue is not Array currentArray
            || nextValue is not Array nextArray)
        {
            return Equals(currentValue, nextValue);
        }

        return currentArray.Length == nextArray.Length
            && Enumerable.Range(0, currentArray.Length)
                .All(i => Equals(currentArray.GetValue(i), nextArray.GetValue(i)));
    }

    /// <summary>
    /// Converts the input value to a string. If a value is null this will default to
    /// "null". If this value is an Array this will iterate through each element and attempt
    /// to invoke the ToString method of said element and return a concatenated string of all
    /// the results.
    /// </summary>
    /// <param name="value">The input value to be stringified.</param>
    /// <returns>A string representation of the input value based on the criteria above.</returns>
    public static string Stringify(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case Array valueArray:
                IEnumerable<string> properties = Enumerable.Range(0, valueArray.Length)
                    .Select(i => valueArray.GetValue(i))
                    .Select(elem => elem?.ToString() ?? "null");
                return $"[{string.Join(", ", properties)}]";
            default:
                return value.ToString() ?? "null";
        }
    }
}
