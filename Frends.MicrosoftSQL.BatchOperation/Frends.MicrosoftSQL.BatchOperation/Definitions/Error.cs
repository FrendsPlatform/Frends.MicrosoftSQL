using System;

namespace Frends.MicrosoftSQL.BatchOperation.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>SQL Server login failed for user 'db_user'.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>SqlException: Login failed for user 'db_user'.</example>
    public Exception AdditionalInfo { get; set; }
}
