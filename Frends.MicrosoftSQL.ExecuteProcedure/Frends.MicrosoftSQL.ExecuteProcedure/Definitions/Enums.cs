namespace Frends.MicrosoftSQL.ExecuteProcedure.Definitions;

/// <summary>
/// SQL transaction isolation levels.
/// </summary>
public enum SqlTransactionIsolationLevel
{
    /// <summary>
    /// A different isolation level than the one specified is being used, but the level cannot be determined.
    /// </summary>
    Unspecified = -1,

    /// <summary>
    /// No transaction.
    /// </summary>
    None,

    /// <summary>
    /// Default is configured by the SQL Server, usually ReadCommited.
    /// </summary>
    Default,

    /// <summary>
    /// Shared locks are held while the data is being read to avoid dirty reads, but the data can be changed before the end of the transaction, resulting in non-repeatable reads or phantom data.
    /// </summary>
    ReadCommitted = 4096,

    /// <summary>
    /// A dirty read is possible, meaning that no shared locks are issued and no exclusive locks are honored.
    /// </summary>
    ReadUncommitted = 256,

    /// <summary>
    /// Locks are placed on all data that is used in a query, preventing other users from updating the data.
    /// Prevents non-repeatable reads but phantom rows are still possible.
    /// </summary>
    RepeatableRead = 65536,

    /// <summary>
    /// A range lock is placed on the System.Data.DataSet, preventing other users from updating or inserting rows into the dataset until the transaction is complete.
    /// </summary>
    Serializable = 1048576,

    /// <summary>
    /// Reduces blocking by storing a version of data that one application can read while another is modifying the same data.
    /// Indicates that from one transaction you cannot see changes made in other transactions, even if you requery.
    /// </summary>
    Snapshot = 16777216,
}

/// <summary>
/// Execute types.
/// </summary>
public enum ExecuteTypes
{
    /// <summary>
    /// Executes a Transact-SQL statement against the connection and returns the number of rows affected.
    /// </summary>
    NonQuery,

    /// <summary>
    /// Executes the query, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.
    /// </summary>
    Scalar,

    /// <summary>
    /// Executes the query, and returns an object that can iterate over the entire result set.
    /// </summary>
    ExecuteReader,
}

/// <summary>
/// SQL Server-specific data type.
/// </summary>
public enum SqlDataTypes
{
    /// <summary>
    /// Automatically infer the SQL Server data type.
    /// </summary>
    Auto = -1,

    /// <summary>
    /// 64-bit integer.
    /// </summary>
    BigInt = 0,

    /// <summary>
    /// Fixed-length binary data.
    /// </summary>
    Binary = 1,

    /// <summary>
    /// Boolean value.
    /// </summary>
    Bit = 2,

    /// <summary>
    /// Fixed-length non-Unicode character data.
    /// </summary>
    Char = 3,

    /// <summary>
    /// Date and time value.
    /// </summary>
    DateTime = 4,

    /// <summary>
    /// Fixed-precision numeric value.
    /// </summary>
    Decimal = 5,

    /// <summary>
    /// Floating-point number.
    /// </summary>
    Float = 6,

    /// <summary>
    /// Variable-length binary data.
    /// </summary>
    Image = 7,

    /// <summary>
    /// 32-bit integer.
    /// </summary>
    Int = 8,

    /// <summary>
    /// Currency value.
    /// </summary>
    Money = 9,

    /// <summary>
    /// Fixed-length Unicode character data.
    /// </summary>
    NChar = 10,

    /// <summary>
    /// Variable-length Unicode character data.
    /// </summary>
    NText = 11,

    /// <summary>
    /// Variable-length Unicode character data.
    /// </summary>
    NVarChar = 12,

    /// <summary>
    /// Single-precision floating-point number.
    /// </summary>
    Real = 13,

    /// <summary>
    /// Globally unique identifier.
    /// </summary>
    UniqueIdentifier = 14,

    /// <summary>
    /// Date and time value with minute precision.
    /// </summary>
    SmallDateTime = 15,

    /// <summary>
    /// 16-bit integer.
    /// </summary>
    SmallInt = 16,

    /// <summary>
    /// Currency value with limited precision.
    /// </summary>
    SmallMoney = 17,

    /// <summary>
    /// Variable-length non-Unicode character data.
    /// </summary>
    Text = 18,

    /// <summary>
    /// Database row version.
    /// </summary>
    Timestamp = 19,

    /// <summary>
    /// 8-bit integer.
    /// </summary>
    TinyInt = 20,

    /// <summary>
    /// Variable-length binary data.
    /// </summary>
    VarBinary = 21,

    /// <summary>
    /// Variable-length non-Unicode character data.
    /// </summary>
    VarChar = 22,

    /// <summary>
    /// Variant value.
    /// </summary>
    Variant = 23,

    /// <summary>
    /// XML data.
    /// </summary>
    Xml = 25,

    /// <summary>
    /// User-defined type.
    /// </summary>
    Udt = 29,

    /// <summary>
    /// Structured data.
    /// </summary>
    Structured = 30,

    /// <summary>
    /// Date value.
    /// </summary>
    Date = 31,

    /// <summary>
    /// Time value.
    /// </summary>
    Time = 32,

    /// <summary>
    /// Date and time value with fractional seconds.
    /// </summary>
    DateTime2 = 33,

    /// <summary>
    /// Date and time value with time zone offset.
    /// </summary>
    DateTimeOffset = 34,
}
