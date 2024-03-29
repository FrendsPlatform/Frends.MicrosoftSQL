﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.MicrosoftSQL.BulkInsert.Definitions;

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
    /// Destination table name.
    /// </summary>
    /// <example>TestTable</example>
    public string TableName { get; set; }

    /// <summary>
    /// Json Array of objects. All object property names need to match with the destination table column names.
    /// </summary>
    /// <example>[{\"Column1\":\"Value1\", \"Column2\":15},{\"Column1\":\"Value2\", \"Column2\":30}]</example>
    [DisplayFormat(DataFormatString = "Json")]
    [DefaultValue("[{\"Column1\":\"Value1\", \"Column2\":15},{\"Column1\":\"Value2\", \"Column2\":30}]")]
    public string InputData { get; set; }
}