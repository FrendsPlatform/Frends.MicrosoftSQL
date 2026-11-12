using Microsoft.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;

internal class Helper
{
    internal static string GetInvalidConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(SqlServerContainer.ConnectionString)
        {
            Password = "WrongPassword123!",
        };
        return builder.ConnectionString;
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
