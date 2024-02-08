using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

/// <summary>
/// Options for CSV outout.
/// </summary>
public class CsvOptions
{
    /// <summary>
    /// Columns to include in the CSV output. Leave empty to include all columns in output.
    /// </summary>
    /// <example>[ column1, column2 ]</example>
    public string[] ColumnsToInclude { get; set; }

    /// <summary>
    /// What to use as field separators.
    /// </summary>
    /// <example>CsvDelimiter.SemiColon</example>
    [DefaultValue(CsvFieldDelimiter.Semicolon)]
    public CsvFieldDelimiter FieldDelimiter { get; set; } = CsvFieldDelimiter.Semicolon;

    /// <summary>
    /// Custom field delimiter as a string.
    /// </summary>
    /// <example>;</example>
    [UIHint(nameof(FieldDelimiter), "", CsvFieldDelimiter.Custom)]
    public string CustomFieldDelimiter { get; set; }

    /// <summary>
    /// What to use as line breaks.
    /// </summary>
    /// <example>CsvLineBreak.CRLF</example>
    [DefaultValue(CsvLineBreak.CRLF)]
    public CsvLineBreak LineBreak { get; set; } = CsvLineBreak.CRLF;

    /// <summary>
    /// Output file encoding.
    /// </summary>
    /// <example>FileEncoding.UTF8</example>
    [DefaultValue(FileEncoding.UTF8)]
    public FileEncoding FileEncoding { get; set; }

    /// <summary>
    /// Enable Bom.
    /// </summary>
    /// <example>true</example>
    [UIHint(nameof(FileEncoding), "", FileEncoding.UTF8)]
    public bool EnableBom { get; set; }

    /// <summary>
    /// File encoding to be used. A partial list of possible encodings: https://en.wikipedia.org/wiki/Windows_code_page#List
    /// </summary>
    /// <example>utf-8</example>
    [UIHint(nameof(FileEncoding), "", FileEncoding.Other)]
    public string EncodingInString { get; set; }

    /// <summary>
    /// Whether to include headers in output.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(true)]
    public bool IncludeHeadersInOutput { get; set; } = true;

    /// <summary>
    /// Whether to sanitize headers in output:
    /// - Strip any chars that are not 0-9, a-z or _
    /// - Make sure that column does not start with a number or underscore.
    /// - Force lower case.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(true)]
    public bool SanitizeColumnHeaders { get; set; } = true;

    /// <summary>
    /// Whether to add quotes around DATE and DATETIME fields.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(true)]
    public bool AddQuotesToDates { get; set; } = true;

    /// <summary>
    /// Whether to add quotes around string typed fields.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(true)]
    public bool AddQuotesToStrings { get; set; } = true;

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
    /// Helper method to return the field delimiter as string.
    /// </summary>
    /// <returns>string</returns>
    internal string GetFieldDelimiterAsString()
    {
        return FieldDelimiter switch
        {
            CsvFieldDelimiter.Comma => ",",
            CsvFieldDelimiter.Pipe => "|",
            CsvFieldDelimiter.Semicolon => ";",
            CsvFieldDelimiter.Custom => CustomFieldDelimiter,
            _ => throw new Exception($"Unknown field delimeter: {FieldDelimiter}"),
        };
    }

    /// <summary>
    /// Helper method to return the line break as string.
    /// </summary>
    /// <returns>string</returns>
    internal string GetLineBreakAsString()
    {
        return LineBreak switch
        {
            CsvLineBreak.CRLF => "\r\n",
            CsvLineBreak.CR => "\r",
            CsvLineBreak.LF => "\n",
            _ => throw new Exception($"Unknown field delimeter: {FieldDelimiter}"),
        };
    }
}