namespace Frends.MicrosoftSQL.BatchOperation.Definitions;

/// <summary>
/// Task's result.
/// </summary>
public class Result
{
    internal Result(bool success = false, int recordsAffected = 0)
    {
        Success = success;
        RecordsAffected = recordsAffected;
    }

    /// <summary>
    /// Operation complete without errors.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Records affected.
    /// </summary>
    /// <example>100</example>
    public int RecordsAffected { get; private set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
