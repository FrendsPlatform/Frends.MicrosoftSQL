namespace Frends.MicrosoftSQL.ExecuteQuery.Definitions;

/// <summary>
/// Task's result.
/// </summary>
public class Result
{
    internal Result(bool success, int recordsAffected, dynamic data)
    {
        Success = success;
        RecordsAffected = recordsAffected;
        Data = data;
        Error = null;
    }

    /// <summary>
    /// Operation complete without errors.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; private set; }

    /// <summary>
    /// Records affected.
    /// Some statements will return -1. See documentation of Input.ExecuteType for more information.
    /// </summary>
    /// <example>100</example>
    public int RecordsAffected { get; private set; }

    /// <summary>
    /// Query result as JToken.
    /// </summary>
    /// <example>
    /// Input.ExecuteType = ExecuteReader: [{"ID": "1","FIRST_NAME": "Saija","LAST_NAME": "Saijalainen","START_DATE": ""}],
    /// Input.ExecuteType = NonQuery: {{  "AffectedRows": -1 }},
    /// Input.ExecuteType = Scalar: {{  "Value": 1 }}
    /// </example>
    public dynamic Data { get; private set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; internal set; }
}
