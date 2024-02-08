namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// Options class usually contains parameters that are required.
/// </summary>
public class Options
{
    /// <summary>
    /// Operation timeout (seconds).
    /// </summary>
    /// <example>30</example>
    [DefaultValue(30)]
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Determines in what format the query is written.
    /// </summary>
    /// <example>ReturnFormat.CSV</example>
    [DefaultValue(ReturnFormat.CSV)]
    public ReturnFormat ReturnFormat { get; set; }

    /// <summary>
    /// Csv options.
    /// </summary>
    [UIHint(nameof(ReturnFormat), "", ReturnFormat.CSV)]
    public CsvOptions CsvOptions { get; set; }
}