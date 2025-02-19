namespace Frends.MicrosoftSQL.BulkInsert.Definitions;

/// <summary>
/// Selection of column mapping options for bulk insert.
/// </summary>
public enum ColumnMapping
{
    /// <summary>
    /// Column mapping is disabled and the bulk insert will insert the data based on the order of the properties in input JSON.
    /// </summary>
    JsonPropertyOrder,

    /// <summary>
    /// Input JSON property names will be used with bulk insert to create column mapping. Column mapping is case sensitive.
    /// </summary>
    JsonPropertyNames,

    /// <summary>
    /// Manual column mapping JSON will be used with bulk insert to create column mapping. Column mapping is case sensitive.
    /// </summary>
    ManualColumnMapping,
}