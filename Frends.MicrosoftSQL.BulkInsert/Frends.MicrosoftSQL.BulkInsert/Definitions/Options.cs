using System.ComponentModel;

namespace Frends.MicrosoftSQL.BulkInsert.Definitions;

/// <summary>
/// Options parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Number of seconds for the operation to complete before it times out.
    /// </summary>
    /// <example>60</example>
    [DefaultValue(60)]
    public int CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Defines the number of rows to be processed before generating a notification event. 
    /// If the number of rows is unknown, NotifyAfter is dynamically set to 10% of the total row count, with a minimum value of 1.
    /// Notification events can be used for error handling to see approximately which row the error occurred.
    /// Default value 0 = There won't be any notifications until the task is completed and Result.Count will be 0.
    /// </summary>
    /// <example>0</example>
    public int NotifyAfter { get; set; }

    /// <summary>
    /// When specified, cause the server to fire the insert triggers for the rows being inserted into the database.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool FireTriggers { get; set; }

    /// <summary>
    /// Preserve source identity values. When not specified, identity values are assigned by the destination.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool KeepIdentity { get; set; }

    /// <summary>
    /// Obtain a bulk update lock for the duration of the bulk copy operation. When not specified, row locks are used.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool TableLock { get; set; }

    /// <summary>
    /// Preserve null values in the destination table regardless of the settings for default values. 
    /// When not specified, null values are replaced by default values where applicable.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool KeepNulls { get; set; }

    /// <summary>
    /// If the input properties have empty values i.e. "", the values will be converted to null if this parameter is set to true.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool ConvertEmptyPropertyValuesToNull { get; set; }

    /// <summary>
    /// (true) Throw an exception or (false) stop the Task and return result object containing Result.Success = false and Result.ErrorMessage = 'exception message'.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; }

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