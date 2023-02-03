using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.MicrosoftSQL.BatchOperation.Definitions;

/// <summary>
/// Input parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Connection string.
    /// </summary>
    /// <example>Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;</example>
    [PasswordPropertyText]
    [DefaultValue("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;")]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Query string for batch operation.
    /// </summary>
    /// <example>INSERT INTO MyTable (id, first_name) VALUES (@id, @first_name)</example>
    [DisplayFormat(DataFormatString = "Sql")]
    public string Query { get; set; }

    /// <summary>
    /// JSON array for batch operation.
    /// </summary>
    /// <example>[{\"Id\":1,\"first_name\":\"Foo\"},{\"Id\":2,\"first_name\":\"Bar\"}]</example>
    [DisplayFormat(DataFormatString = "Json")]
    public string InputJson { get; set; }
}