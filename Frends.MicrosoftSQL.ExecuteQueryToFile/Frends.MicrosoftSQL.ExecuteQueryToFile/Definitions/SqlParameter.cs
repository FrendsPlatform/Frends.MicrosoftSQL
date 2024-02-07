using System.ComponentModel;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

/// <summary>
/// Sql query parameter class.
/// </summary>
public class SqlParameter
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    /// <example>first_name</example>
    public string Name { get; set; }

    /// <summary>
    /// The value of the parameter.
    /// </summary>
    /// <example>FirstName</example>
    public object Value { get; set; }

    /// <summary>
    /// SQL Server-specific data type.
    /// Note! Use SqlDataType.Auto if not sure of the type.
    /// See https://learn.microsoft.com/en-us/dotnet/api/system.data.sqldbtype?view=net-7.0 for more information.
    /// </summary>
    /// <example>SqlDbTypes.Empty</example>
    [DefaultValue(SqlDataTypes.Auto)]
    public SqlDataTypes SqlDataType { get; set; }
}