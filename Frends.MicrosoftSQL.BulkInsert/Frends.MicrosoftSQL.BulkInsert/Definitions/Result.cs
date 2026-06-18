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
    /// Number of processed rows.
    /// In case of failure it shows notified number of processed rows. Approximation logic is defined by Options.NotifyAfter
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
