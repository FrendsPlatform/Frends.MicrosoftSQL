using System;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Definitions;

/// <summary>
/// Error that occurred during task execution.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>Stored procedure execution failed due to a SQL Server timeout.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>object { Exception AdditionalInfo }</example>
    public Exception AdditionalInfo { get; set; }
}
