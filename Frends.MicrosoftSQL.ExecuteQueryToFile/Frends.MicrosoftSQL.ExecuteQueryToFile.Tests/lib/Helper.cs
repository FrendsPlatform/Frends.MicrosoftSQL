using System.Data;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;
using Microsoft.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests.Lib;

internal static class Helper
{
    internal static string GetConnectionString() => SqlServerContainer.ConnectionString;

    internal static void CreateTestTable(string connString, string tableName, string query = null)
    {
        using var connection = new SqlConnection(connString);
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = query ?? $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{tableName}') BEGIN CREATE TABLE {tableName} ( Id int, LastName varchar(255), FirstName varchar(255), Salary decimal(6,2), Image Image, TestText VarBinary(MAX), TestNull Varchar(255)); END";
        command.ExecuteNonQuery();
    }

    internal static void ExecuteNonQuery(string connString, string commandText, SqlParameter[] parameters = null)
    {
        using var connection = new SqlConnection(connString);
        connection.Open();
        using var command = new SqlCommand
        {
            CommandText = commandText,
            CommandType = CommandType.Text,
            CommandTimeout = 30,
            Connection = connection,
        };
        if (parameters != null)
        {
            foreach (var parameter in parameters)
                command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }
}
