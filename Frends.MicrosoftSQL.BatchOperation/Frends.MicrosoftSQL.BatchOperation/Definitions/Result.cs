namespace Frends.MicrosoftSQL.BatchOperation.Definitions;

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
    /// Records affected.
    /// </summary>
    /// <example>100</example>
    public int RecordsAffected { get; set; }

    /// <summary>
    /// Error message.
    /// This value is generated when an exception occurs and Options.ThrowErrorOnFailure = false.
    /// </summary>
    /// <example>Login failed for user 'user'.</example>
    public string ErrorMessage => Error?.Message;

    /// <summary>
    /// Error that occurred during the task.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
