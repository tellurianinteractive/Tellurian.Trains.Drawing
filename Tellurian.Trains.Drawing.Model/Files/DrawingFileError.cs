namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>Why a drawing file couldn't be read.</summary>
public enum DrawingFileError
{
    /// <summary>The file isn't valid JSON.</summary>
    InvalidJson,

    /// <summary>The JSON isn't a Tellurian drawing file: the envelope is missing or wrong.</summary>
    NotADrawingFile,

    /// <summary>The file is a drawing file of another kind than expected.</summary>
    WrongKind,

    /// <summary>The file was written by a newer version of the app (spec §6: refused with a clear message).</summary>
    NewerSchemaVersion,

    /// <summary>The content doesn't match the schema.</summary>
    InvalidContent,
}
