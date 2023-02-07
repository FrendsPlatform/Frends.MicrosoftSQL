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
    public bool Success { get; private set; }

    /// <summary>
    /// Records affected.
    /// </summary>
    /// <example>100</example>
    public int RecordsAffected { get; private set; }

    /// <summary>
    /// Error message.
    /// This value is generated when an exception occurs and Options.ThrowErrorOnFailure = false.
    /// </summary>
    /// <example>Login failed for user 'user'.</example>
    public string ErrorMessage { get; private set; }

    internal Result(bool success, int recordsAffected, string errorMessage)
    {
        Success = success;
        RecordsAffected = recordsAffected;
        ErrorMessage = errorMessage;
    }
}