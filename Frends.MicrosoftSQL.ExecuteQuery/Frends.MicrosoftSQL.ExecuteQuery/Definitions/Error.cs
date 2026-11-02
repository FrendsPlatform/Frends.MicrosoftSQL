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
    /// <example>Invalid column name 'OrderId'.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>Microsoft.Data.SqlClient.SqlException</example>
    public Exception AdditionalInfo { get; set; }
}
