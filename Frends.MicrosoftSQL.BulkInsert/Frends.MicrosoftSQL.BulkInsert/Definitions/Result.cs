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
    public bool Success { get; private set; }

    /// <summary>
    /// Number of processed rows reported by SqlBulkCopy notifications.
    /// The value is approximate and can be rounded down to the nearest NotifyAfter interval (or 0 if no notification is raised).
    /// </summary>
    /// <example>100</example>
    public long Count { get; private set; }

    /// <summary>
    /// Error message.
    /// This value is generated when an exception occurs and Options.ThrowErrorOnFailure = false.
    /// </summary>
    /// <example>Login failed for user 'user'.</example>
    public string ErrorMessage { get; private set; }

    internal Result(bool success, long count, string errorMessage)
    {
        Success = success;
        Count = count;
        ErrorMessage = errorMessage;
    }
}
