using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;

internal class Helper
{

    private static readonly string _connectionString = Environment.GetEnvironmentVariable(
    "FRENDS_MICROSOFTSQL_CONNSTRING");
    private static readonly string _userId = Environment.GetEnvironmentVariable(
        "FRENDS_MICROSOFTSQL_USERID");

    internal static string CreateConnectionString()
    {
        return _connectionString;
    }

    internal static string GetUserName()
    {
        return _userId;
    }

    internal static string GetInvalidConnectionString()
    {
        return Regex.Replace(_connectionString, @"Password=.*?;", $"Password={Guid.NewGuid().ToString()};");
    }

    internal static int GetRowCount(string connString, string table)
    {
        using var connection = new SqlConnection(connString);
        connection.Open();
        var getRows = connection.CreateCommand();
        getRows.CommandText = $"SELECT COUNT(*) FROM {table}";
        var count = (int)getRows.ExecuteScalar();
        connection.Close();
        connection.Dispose();
        return count;
    }
}