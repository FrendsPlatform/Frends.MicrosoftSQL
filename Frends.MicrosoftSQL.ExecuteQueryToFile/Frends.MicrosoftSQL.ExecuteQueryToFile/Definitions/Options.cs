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
    /// <example>{ "FieldDelimiter": "Semicolon" }</example>
    [UIHint(nameof(ReturnFormat), "", ReturnFormat.CSV)]
    public CsvOptions CsvOptions { get; set; }

    /// <summary>
    /// Json options.
    /// </summary>
    /// <example>{ "JsonOutputMode": "Indented" }</example>
    [UIHint(nameof(ReturnFormat), "", ReturnFormat.JSON)]
    public JsonOptions JsonOptions { get; set; } = new JsonOptions();

    /// <summary>
    /// Whether to throw an error when the task fails.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message when the task fails.
    /// </summary>
    /// <example>SQL query export failed.</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}