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
    /// Defines the number of rows to be processed before generating a notification event. Range: 0 - 'count of rows to be processed'
    /// Notification event can be used for error handling to see approximately which row the error happened.
    /// Default value 0 = There won't be any notifications until the task is completed.
    /// 10 = The counter is updated after every 10 rows or when every row has been processed.
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
    /// Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Default is Serializable.
    /// </summary>
    /// <example>SqlTransactionIsolationLevel.ReadCommitted</example>
    [DefaultValue(SqlTransactionIsolationLevel.ReadCommitted)]
    public SqlTransactionIsolationLevel SqlTransactionIsolationLevel { get; set; }
}