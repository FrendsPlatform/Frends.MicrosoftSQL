namespace Frends.MicrosoftSQL.ExecuteQuery.Definitions;

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
    /// ExecuteReader for SELECT-query and NonQuery for UPDATE, INSERT, or DELETE statements.
    /// </summary>
    Auto,

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
    ExecuteReader
}

/// <summary>
/// SQL Server-specific data type.
/// </summary>
public enum SqlDataTypes
{
#pragma warning disable CS1591 // self explanatory
    Auto = -1,
    BigInt = 0,
    Binary = 1,
    Bit = 2,
    Char = 3,
    DateTime = 4,
    Decimal = 5,
    Float = 6,
    Image = 7,
    Int = 8,
    Money = 9,
    NChar = 10,
    NText = 11,
    NVarChar = 12,
    Real = 13,
    UniqueIdentifier = 14,
    SmallDateTime = 15,
    SmallInt = 16,
    SmallMoney = 17,
    Text = 18,
    Timestamp = 19,
    TinyInt = 20,
    VarBinary = 21,
    VarChar = 22,
    Variant = 23,
    Xml = 25,
    Udt = 29,
    Structured = 30,
    Date = 31,
    Time = 32,
    DateTime2 = 33,
    DateTimeOffset = 34
#pragma warning restore CS1591 // self explanatory
}