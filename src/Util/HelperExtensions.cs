namespace ImageViewer.Util;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ImageViewer.Models;

/// <summary>
/// Common extensions to simplify some repeated code.
/// </summary>
public static class HelperExtensions
{
    /// <summary>
    /// Returns a subset of the source enumerable such that each resulting element in the
    /// enumerable has an <see cref="IPathResource.Path"/> equal to the input path.
    /// </summary>
    /// <typeparam name="T">The enumerable collection type. Must be of type <see cref="IPathResource"/>.</typeparam>
    /// <param name="source">The collection to filter on.</param>
    /// <param name="absolutePath">The path to match the AbsolutePath of each element of the collection
    /// against.</param>
    /// <returns>An enumerable collection in which each element in the collection has an AbsolutePath
    /// matching the input path.</returns>
    public static IEnumerable<T> WhereByPath<T>(this IEnumerable<T> source, PathLike? absolutePath)
    where T : IPathResource => source.Where(s => s.Path.Equals(absolutePath));

    /// <summary>
    /// Returns the first element in the enumerable in which the element has an
    /// <see cref="IPathResource.Path"/> matching the input absolute path.
    /// </summary>
    /// <typeparam name="T">The enumerable collection type. Must be of type <see cref="IPathResource"/>.</typeparam>
    /// <param name="source">The collection to filter on.</param>
    /// <param name="absolutePath">The path to match the AbsolutePath of each element on.</param>
    /// <returns>The first element with a matching path or null.</returns>
    public static T? FirstByPath<T>(this IEnumerable<T> source, PathLike? absolutePath)
    where T : IPathResource => source.WhereByPath(absolutePath).FirstOrDefault();

    /// <summary>
    /// Creates a property changed event handler that invokes a provided callback
    /// when a property, associated with the callback, has been changed on the desired
    /// sender object.
    /// </summary>
    /// <param name="desiredSender">The callbacks provided in the mappings param should only be checked
    /// and invoked if the sender of the event is strictly equal to this.</param>
    /// <param name="mappings">A dictionary in which the keys represent property names and the values
    /// represent an action to be invoked when the event indicates the associated property has
    /// been changed.</param>
    /// <returns>A <see cref="PropertyChangedEventHandler"/>.</returns>
    public static PropertyChangedEventHandler CreatePropertyChangeConsumer(
        object desiredSender, Dictionary<string, Action> mappings)
    {
        return (sender, args) =>
        {
            if (sender != desiredSender || args.PropertyName == null)
            {
                return;
            }

            if (mappings.TryGetValue(args.PropertyName, out Action? action))
            {
                action.Invoke();
            }
        };
    }

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
                IEnumerable<string> result = Enumerable.Range(0, valueArray.Length)
                    .Select(i => valueArray.GetValue(i))
                    .Select(elem => elem?.ToString() ?? "null");
                return $"[{string.Join(", ", result)}]";
            default:
                return value.ToString() ?? "null";
        }
    }
}
