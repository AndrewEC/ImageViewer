namespace ImageViewer.Log;

using System;

/// <summary>
/// Simplistic proxy logger to log messages using <see cref="Console.WriteLine()"/>.
/// </summary>
/// <typeparam name="T">The type of the class that will be using this logger instance.</typeparam>
public sealed class ConsoleLogger<T>
{
    private static readonly string LogMessageTemplate = "[{0}][{1}] -> [{2}]";
    private static readonly string ErrorMessageTemplate = "{0} :: {1}";
    private static readonly string TimestampFormat = "HH:mm:ss";

    private readonly string parentName = typeof(T).Name;

    /// <summary>
    /// Formats and logs the message.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void Log(string message)
    {
        string timestamp = DateTime.Now.ToString(TimestampFormat);
        string logMessage = string.Format(LogMessageTemplate, timestamp, parentName, message);
        Console.WriteLine(logMessage);
    }

    /// <summary>
    /// Formats and logs the message and error stack trace.
    /// </summary>
    /// <param name="prefix">A message to prefix onto the stack trace being logged.</param>
    /// <param name="error">The error from which the stack trace will be pulled.</param>
    public void Error(string prefix, Exception? error)
        => Log(string.Format(ErrorMessageTemplate, prefix, error?.StackTrace ?? "No stack trace available."));
}