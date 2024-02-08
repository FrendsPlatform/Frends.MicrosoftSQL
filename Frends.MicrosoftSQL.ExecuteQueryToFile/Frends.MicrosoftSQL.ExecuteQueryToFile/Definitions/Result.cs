namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

/// <summary>
/// Result class usually contains properties of the return object.
/// </summary>
public class Result
{
    internal Result(int entriesWritten, string path, string name)
    {
        EntriesWritten = entriesWritten;
        Path = path;
        FileName = name;
    }

    internal Result()
    {
    }

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
}
