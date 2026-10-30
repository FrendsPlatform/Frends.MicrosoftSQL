namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// CSV field delimeter options.
/// </summary>
public enum CsvFieldDelimiter
{
    /// <summary>
    /// Separates fields with commas.
    /// </summary>
    Comma,

    /// <summary>
    /// Separates fields with semicolons.
    /// </summary>
    Semicolon,

    /// <summary>
    /// Separates fields with vertical bars.
    /// </summary>
    Pipe,

    /// <summary>
    /// Uses the custom delimiter configured in the CSV options.
    /// </summary>
    Custom,
}
