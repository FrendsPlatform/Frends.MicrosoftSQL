namespace Frends.MicrosoftSQL.BatchOperation.Definitions;

/// <summary>
/// Task's result.
/// </summary>
public class Result
{
    private Error error;

    internal Result(bool success = false, int recordsAffected = 0, string errorMessage = null)
    {
        Success = success;
        RecordsAffected = recordsAffected;
        ErrorMessage = errorMessage;
        if (errorMessage is not null)
            Error = new Error { Message = errorMessage };
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
    /// Error message.
    /// This value is generated when an exception occurs and Options.ThrowErrorOnFailure = false.
    /// </summary>
    /// <example>Login failed for user 'user'.</example>
    public string ErrorMessage { get; private set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error
    {
        get => error;
        set
        {
            error = value;
            if (value is not null)
                ErrorMessage = value.Message;
        }
    }
}
