using System.ComponentModel;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

/// <summary>
/// Options for writing SQL query results to a JSON file.
/// </summary>
public class JsonOptions
{
    /// <summary>
    /// How the JSON output is structured.
    /// </summary>
    /// <example>JsonOutputMode.Indented</example>
    [DefaultValue(JsonOutputMode.Indented)]
    public JsonOutputMode JsonOutputMode { get; set; } = JsonOutputMode.Indented;

    /// <summary>
    /// Date format to use for formatting DATE columns, use .NET formatting tokens.
    /// Note that formatting is done using invariant culture.
    /// </summary>
    /// <example>yyyy-MM-dd</example>
    [DefaultValue("\"yyyy-MM-dd\"")]
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Date format to use for formatting DATETIME columns, use .NET formatting tokens.
    /// Note that formatting is done using invariant culture.
    /// </summary>
    /// <example>yyyy-MM-dd HH:mm:ss</example>
    [DefaultValue("\"yyyy-MM-dd HH:mm:ss\"")]
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Format for SQL time columns (mapped to TimeSpan in .NET).
    /// Default: ISO 8601 time (HH:mm:ss.fff).
    /// </summary>
    /// <example>hh\:mm\:ss\.fff</example>
    [DefaultValue(@"hh\:mm\:ss\.fff")]
    public string TimeFormat { get; set; } = @"hh\:mm\:ss\.fff";

    /// <summary>
    /// Format for SQL datetimeoffset columns (mapped to DateTimeOffset in .NET).
    /// Default: ISO 8601 round-trip format preserving the UTC offset.
    /// </summary>
    /// <example>0</example>
    [DefaultValue(0)]
    public string DateTimeOffsetFormat { get; set; } = "O";

    /// <summary>
    /// When true, SQL NULL values for string columns are written as empty strings instead of JSON null.
    /// Non-string types such as numbers and booleans are always written as JSON null regardless of this setting.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool HandleNullAsEmpty { get; set; } = false;

    /// <summary>
    /// Size in bytes of the file write buffer. Larger values reduce I/O syscalls for big exports;
    /// smaller values reduce memory usage. Default: 65536 (64 KB).
    /// </summary>
    /// <example>65536</example>
    [DefaultValue(65536)]
    public int FileBufferSize { get; set; } = 65536;
}
