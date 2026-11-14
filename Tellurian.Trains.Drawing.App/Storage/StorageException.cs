namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>The browser's storage couldn't be used, e.g. because it is blocked or full.</summary>
public sealed class StorageException(string message, Exception? innerException = null) : Exception(message, innerException);
