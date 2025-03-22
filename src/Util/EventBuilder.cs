namespace ImageView.Util;

using System;
using System.Collections.Generic;
using System.ComponentModel;

/// <summary>
/// Static utility to create event handlers.
/// </summary>
public static class EventBuilder
{
    /// <summary>
    /// Creates a <see cref="PropertyChangedEventHandler"/> delegate function that will
    /// compare the changed property name with a key in the <paramref name="mappings"/> and invoke
    /// the associated <see cref="Action"/> if one is found.
    /// </summary>
    /// <param name="desiredSender">No check against the input mapping should be performed, and
    /// no action invoked, if the sender of the event does not equal the desired sender.</param>
    /// <param name="mappings">A dictionary in which the keys represent property names and the values
    /// represent an <see cref="Action"/> to be invoked when the event indicates the associated
    /// property has been changed.</param>
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
}