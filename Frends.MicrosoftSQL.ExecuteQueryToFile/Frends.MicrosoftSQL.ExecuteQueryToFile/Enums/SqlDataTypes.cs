namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// SQL Server-specific data type.
/// </summary>
public enum SqlDataTypes
{
    /// <summary>
    /// Infers the SQL type from the parameter value.
    /// </summary>
    Auto = -1,

    /// <summary>
    /// Uses the SQL Server bigint type.
    /// </summary>
    BigInt = 0,

    /// <summary>
    /// Uses the SQL Server binary type.
    /// </summary>
    Binary = 1,

    /// <summary>
    /// Uses the SQL Server bit type.
    /// </summary>
    Bit = 2,

    /// <summary>
    /// Uses the SQL Server char type.
    /// </summary>
    Char = 3,

    /// <summary>
    /// Uses the SQL Server datetime type.
    /// </summary>
    DateTime = 4,

    /// <summary>
    /// Uses the SQL Server decimal type.
    /// </summary>
    Decimal = 5,

    /// <summary>
    /// Uses the SQL Server float type.
    /// </summary>
    Float = 6,

    /// <summary>
    /// Uses the SQL Server image type.
    /// </summary>
    Image = 7,

    /// <summary>
    /// Uses the SQL Server int type.
    /// </summary>
    Int = 8,

    /// <summary>
    /// Uses the SQL Server money type.
    /// </summary>
    Money = 9,

    /// <summary>
    /// Uses the SQL Server nchar type.
    /// </summary>
    NChar = 10,

    /// <summary>
    /// Uses the SQL Server ntext type.
    /// </summary>
    NText = 11,

    /// <summary>
    /// Uses the SQL Server nvarchar type.
    /// </summary>
    NVarChar = 12,

    /// <summary>
    /// Uses the SQL Server real type.
    /// </summary>
    Real = 13,

    /// <summary>
    /// Uses the SQL Server uniqueidentifier type.
    /// </summary>
    UniqueIdentifier = 14,

    /// <summary>
    /// Uses the SQL Server smalldatetime type.
    /// </summary>
    SmallDateTime = 15,

    /// <summary>
    /// Uses the SQL Server smallint type.
    /// </summary>
    SmallInt = 16,

    /// <summary>
    /// Uses the SQL Server smallmoney type.
    /// </summary>
    SmallMoney = 17,

    /// <summary>
    /// Uses the SQL Server text type.
    /// </summary>
    Text = 18,

    /// <summary>
    /// Uses the SQL Server timestamp type.
    /// </summary>
    Timestamp = 19,

    /// <summary>
    /// Uses the SQL Server tinyint type.
    /// </summary>
    TinyInt = 20,

    /// <summary>
    /// Uses the SQL Server varbinary type.
    /// </summary>
    VarBinary = 21,

    /// <summary>
    /// Uses the SQL Server varchar type.
    /// </summary>
    VarChar = 22,

    /// <summary>
    /// Uses the SQL Server sql_variant type.
    /// </summary>
    Variant = 23,

    /// <summary>
    /// Uses the SQL Server xml type.
    /// </summary>
    Xml = 25,

    /// <summary>
    /// Uses the SQL Server user-defined type.
    /// </summary>
    Udt = 29,

    /// <summary>
    /// Uses a structured table-valued parameter type.
    /// </summary>
    Structured = 30,

    /// <summary>
    /// Uses the SQL Server date type.
    /// </summary>
    Date = 31,

    /// <summary>
    /// Uses the SQL Server time type.
    /// </summary>
    Time = 32,

    /// <summary>
    /// Uses the SQL Server datetime2 type.
    /// </summary>
    DateTime2 = 33,

    /// <summary>
    /// Uses the SQL Server datetimeoffset type.
    /// </summary>
    DateTimeOffset = 34,
}