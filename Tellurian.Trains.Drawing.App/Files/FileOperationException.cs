namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>A file couldn't be opened, read or saved, e.g. because permission was denied.</summary>
public sealed class FileOperationException(string message, Exception? innerException = null) : Exception(message, innerException);
