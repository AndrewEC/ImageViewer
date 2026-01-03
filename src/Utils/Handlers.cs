namespace ImageViewer.Utils;

using System;
using System.Collections.Generic;
using System.ComponentModel;

public static class Handlers
{
    public static void RegisterPropertyChangeHandler(
        INotifyPropertyChanged targetProducer,
        Dictionary<string, Action> callbacks)
    {
        targetProducer.PropertyChanged += (sender, e) =>
        {
            if (sender != targetProducer)
            {
                return;
            }

            string propertyName = e.PropertyName ?? string.Empty;
            if (callbacks.TryGetValue(propertyName, out Action? action))
            {
                action.Invoke();
            }
        };
    }
}