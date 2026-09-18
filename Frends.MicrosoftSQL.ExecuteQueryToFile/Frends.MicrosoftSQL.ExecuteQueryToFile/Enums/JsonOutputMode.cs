namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// Specifies the format used when writing JSON output.
/// </summary>
public enum JsonOutputMode
{
    /// <summary>
    /// Writes the result as a single indented JSON array containing all rows.
    /// </summary>
    Indented,

    /// <summary>
    /// Writes the result as JSON Lines (newline-delimited JSON), with each row on a separate line.
    /// </summary>
    JsonLines,
}
