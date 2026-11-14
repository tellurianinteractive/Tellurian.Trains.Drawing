namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>A drawing file couldn't be read; <see cref="Error"/> tells why.</summary>
public sealed class DrawingFileException(DrawingFileError error, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public DrawingFileError Error { get; } = error;
}
