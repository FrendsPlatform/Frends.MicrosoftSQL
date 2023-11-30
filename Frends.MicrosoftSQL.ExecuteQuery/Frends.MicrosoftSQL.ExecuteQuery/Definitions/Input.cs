using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.MicrosoftSQL.ExecuteQuery.Definitions;

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
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;")]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Query to be executed in string format.
    /// Supports SELECT, INSERT, UPDATE and DELETE statements.
    /// </summary>
    /// <example>
    /// "SELECT * FROM MyTable", 
    /// "INSERT INTO MyTable (id, first_name, last_name) VALUES (:id, :first_name, :last_name)", 
    /// </example>
    [DisplayFormat(DataFormatString = "Sql")]
    public string Query { get; set; }

    /// <summary>
    /// Parameters for the database query.
    /// </summary>
    /// <example>[
    /// { Name = "id", Value = 1, DataType = SqlDataTypes.Auto },
    /// { Name = "first_name", Value = "John", DataType = SqlDataTypes.NVarChar },
    /// { Name = "last_name", Value = "Doe", DataType = SqlDataTypes.NVarChar }
    /// ]</example>
    public QueryParameter[] Parameters { get; set; }

    /// <summary>
    /// Specifies how a command string is interpreted.
    /// Auto: ExecuteReader for SELECT-query and NonQuery for UPDATE, INSERT, or DELETE statements.
    /// ExecuteReader: Use this operation to execute any arbitrary SQL statements in SQL Server if you want the result set to be returned.
    /// NonQuery: Use this operation to execute any arbitrary SQL statements in SQL Server if you do not want any result set to be returned. You can use this operation to create database objects or change data in a database by executing UPDATE, INSERT, or DELETE statements. The return value of this operation is of Int32 data type, and For the UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the SQL statement. For all other types of statements, the return value is -1.
    /// Scalar: Use this operation to execute any arbitrary SQL statements in SQL Server to return a single value. This operation returns the value only in the first column of the first row in the result set returned by the SQL statement.
    /// </summary>
    /// <example>ExecuteType.ExecuteReader</example>
    [DefaultValue(ExecuteTypes.ExecuteReader)]
    public ExecuteTypes ExecuteType { get; set; }
}

/// <summary>
/// Query parameter.
/// </summary>
public class QueryParameter
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