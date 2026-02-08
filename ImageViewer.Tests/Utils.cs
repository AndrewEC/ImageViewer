namespace ImageViewer.Tests;

using Avalonia.Controls;
using Avalonia.VisualTree;

public static class Utils
{
    public static TControl FindNestedControl<TViewControl, TControl>(Window window, string nestedControlName)
    where TViewControl : Control
    where TControl : Control
    {
        TViewControl? view = window.FindDescendantOfType<TViewControl>();
        Assert.That(view, Is.Not.Null, $"Could not find descendant of type [{typeof(TViewControl).Name}].");

        TControl? control = view.FindControl<TControl>(nestedControlName);
        Assert.That(control, Is.Not.Null, $"Could not find control of type [{typeof(TControl).Name}] nested within [{typeof(TViewControl).Name}].");

        return control;
    }
}