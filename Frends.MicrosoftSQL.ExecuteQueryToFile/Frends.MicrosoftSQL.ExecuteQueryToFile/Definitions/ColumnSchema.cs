using System;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

/// <summary>
/// Represents schema metadata for a single database column, including ordinal position, column name, .NET type, and
/// database type name.
/// </summary>
/// <remarks>Instances are immutable and expose read-only properties initialized in the constructor.</remarks>
public sealed class ColumnSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnSchema"/> class.
    /// </summary>
    /// <param name="index">The zero-based ordinal position of the column in the result set.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="dotnetType">The .NET type of the column.</param>
    /// <param name="dbTypeName">The database type name of the column.</param>
    public ColumnSchema(int index, string name, Type dotnetType, string dbTypeName)
    {
        Index = index;
        Name = name;
        DotnetType = dotnetType;
        DbTypeName = dbTypeName;
    }

    /// <summary>
    /// Gets the zero-based ordinal position of the column in the result set.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the .NET type of the column.
    /// </summary>
    public Type DotnetType { get; }

    /// <summary>
    /// Gets the database type name of the column.
    /// </summary>
    public string DbTypeName { get; }
}
