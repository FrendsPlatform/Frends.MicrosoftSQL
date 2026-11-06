namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// CSV line break options.
/// </summary>
public enum CsvLineBreak
{
    /// <summary>
    /// Uses a carriage return followed by a line feed.
    /// </summary>
    CRLF,

    /// <summary>
    /// Uses a line feed.
    /// </summary>
    LF,

    /// <summary>
    /// Uses a carriage return.
    /// </summary>
    CR,
}
