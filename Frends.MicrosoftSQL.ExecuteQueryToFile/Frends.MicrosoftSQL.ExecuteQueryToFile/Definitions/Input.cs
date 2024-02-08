namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Input class usually contains parameters that are required.
/// </summary>
public class Input
{
    /// <summary>
    /// Query to execute.
    /// </summary>
    /// <example>SELECT * FROM table</example>
    [DisplayFormat(DataFormatString = "Sql")]
    public string Query { get; set; }

    /// <summary>
    /// Query parameters.
    /// </summary>
    /// <example>[ { Name = test, Value = test_value, SqlDataType = SqlDataTypes.Auto } ]</example>
    public SqlParameter[] QueryParameters { get; set; }

    /// <summary>
    /// Database connection string.
    /// </summary>
    /// <example>Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;</example>
    [DefaultValue("\"Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;\"")]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Output file path.
    /// </summary>
    /// <example>C:\path\tp\file.csv</example>
    [DefaultValue("")]
    public string OutputFilePath { get; set; }
}