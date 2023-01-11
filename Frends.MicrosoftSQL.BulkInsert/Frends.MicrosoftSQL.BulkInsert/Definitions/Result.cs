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
    /// Count of processed rows.
    /// </summary>
    /// <example>100</example>
    public long Count { get; private set; }

    internal Result(bool success, long count)
    {
        Success = success;
        Count = count;
    }
}