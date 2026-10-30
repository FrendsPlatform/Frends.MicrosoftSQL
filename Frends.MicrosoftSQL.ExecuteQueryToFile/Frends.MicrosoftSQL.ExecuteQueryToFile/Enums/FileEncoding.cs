namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// File encoding used to encode the file.
/// </summary>
public enum FileEncoding
{
    /// <summary>
    /// Uses UTF-8 encoding.
    /// </summary>
    UTF8,

    /// <summary>
    /// Uses the system default encoding.
    /// </summary>
    ANSI,

    /// <summary>
    /// Uses ASCII encoding.
    /// </summary>
    ASCII,

    /// <summary>
    /// Uses UTF-16 little-endian encoding.
    /// </summary>
    Unicode,

    /// <summary>
    /// Uses the encoding specified by its name in the CSV options.
    /// </summary>
    Other,
}
