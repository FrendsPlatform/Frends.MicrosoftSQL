using System.ComponentModel;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Definitions;

/// <summary>
/// Options parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// (true) Throw an exception or (false) stop the Task and return result object containing Result.Success = false and Result.ErrorMessage = 'exception message'.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; }

    /// <summary>
    /// Number of seconds for the operation to complete before it times out.
    /// </summary>
    /// <example>60</example>
    [DefaultValue(60)]
    public int CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Starts a database transaction with the specified isolation level.
    /// Isolation evel specifies the transaction locking behavior for the connection.
    /// None: No transaction is set up so there won't be a rollback if exception occurs.
    /// Default: Default is configured by the SQL Server, usually ReadCommited.
    /// ReadCommitted: (Default value in most of the SQL Servers). Shared locks are held while the data is being read to avoid dirty reads, but the data can be changed before the end of the transaction, resulting in non-repeatable reads or phantom data.
    /// Unspecified: A different isolation level than the one specified is being used, but the level cannot be determined.
    /// ReadUncommitted: A dirty read is possible, meaning that no shared locks are issued and no exclusive locks are honored.
    /// RepeatableRead: Locks are placed on all data that is used in a query, preventing other users from updating the data.Prevents non-repeatable reads but phantom rows are still possible.
    /// Serializable: A range lock is placed on the System.Data.DataSet, preventing other users from updating or inserting rows into the dataset until the transaction is complete.
    /// Snapshot: Reduces blocking by storing a version of data that one application can read while another is modifying the same data. Indicates that from one transaction you cannot see changes made in other transactions, even if you requery.
    /// </summary>
    /// <example>SqlTransactionIsolationLevel.ReadCommitted</example>
    [DefaultValue(SqlTransactionIsolationLevel.ReadCommitted)]
    public SqlTransactionIsolationLevel SqlTransactionIsolationLevel { get; set; }
}