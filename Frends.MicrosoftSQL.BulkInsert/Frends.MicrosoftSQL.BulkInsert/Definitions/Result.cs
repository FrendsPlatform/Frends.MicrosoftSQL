namespace Frends.MicrosoftSQL.BulkInsert.Definitions;

/// <summary>
/// Task's result.
/// </summary>
public class Result
{
    /// <summary>
    /// Operation complete without errors.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Number of processed rows.
    /// In case of failure it shows notified number of processed rows. Approximation logic is defined by Options.NotifyAfter
    /// </summary>
    /// <example>100</example>
    public long Count { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
