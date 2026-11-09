using Frends.MicrosoftSQL.ExecuteProcedure.Tests.Lib;
using Microsoft.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

internal static class Helper
{
    internal static string GetConnectionString() => SqlServerContainer.ConnectionString;

    internal static string GetInvalidConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(GetConnectionString()) { Password = "WrongPassWord" };
        return builder.ConnectionString;
    }

    internal static Task<Definitions.Result> ExecuteProcedure(
        Definitions.Input input,
        Definitions.Options options,
        CancellationToken cancellationToken = default,
        Definitions.Connection? connection = null) =>
        MicrosoftSQL.ExecuteProcedure(
            input,
            connection ?? new Definitions.Connection { ConnectionString = GetConnectionString() },
            options,
            cancellationToken);

    internal static int GetRowCount(string tableName)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return (int)command.ExecuteScalar();
    }

    internal static void ExecuteNonQuery(string command)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        using var sqlCommand = connection.CreateCommand();
        sqlCommand.CommandText = command;
        sqlCommand.ExecuteNonQuery();
    }
}
