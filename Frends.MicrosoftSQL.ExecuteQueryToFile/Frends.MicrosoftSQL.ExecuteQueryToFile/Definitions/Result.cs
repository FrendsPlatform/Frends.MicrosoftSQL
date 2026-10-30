namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

/// <summary>
/// Result class usually contains properties of the return object.
/// </summary>
public class Result
{
    internal Result(int entriesWritten, string path, string name)
    {
        Success = true;
        EntriesWritten = entriesWritten;
        Path = path;
        FileName = name;
    }

    internal Result()
    {
    }

    /// <summary>
    /// Indicates whether the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; private set; }

    /// <summary>
    /// Amount of entries written.
    /// </summary>
    /// <example>2</example>
    public int EntriesWritten { get; private set; }

    /// <summary>
    /// Path to the file.
    /// </summary>
    /// <example>C:\test.csv</example>
    public string Path { get; set; }

    /// <summary>
    /// Name of the file.
    /// </summary>
    /// <example>test.csv</example>
    public string FileName { get; set; }

    /// <summary>
    /// Error message generated when the task fails.
    /// </summary>
    /// <example>Login failed for user 'sqluser'.</example>
    public string Error { get; private set; }
}
