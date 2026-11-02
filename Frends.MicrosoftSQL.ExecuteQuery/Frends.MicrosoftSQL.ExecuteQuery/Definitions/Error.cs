using System;

namespace Frends.MicrosoftSQL.ExecuteQuery.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>SQL query failed: Invalid object name 'Orders'.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>SqlException with error code 208.</example>
    public Exception AdditionalInfo { get; set; }
}
