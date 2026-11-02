namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Connection settings for the Microsoft SQL database.
/// </summary>
public class Connection
{
    /// <summary>
    /// Database connection string.
    /// </summary>
    /// <example>Server=myServerAddress;Database=myDataBase;User Id=myUsername;******;</example>
    [DefaultValue("\"Server=myServerAddress;Database=myDataBase;User Id=myUsername;******;\"")]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ConnectionString { get; set; }
}
