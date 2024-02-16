namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

using System.Data.SqlClient;

internal static class Helper
{
    internal static string GetConnectionString()
    {
        var user = "SA";
        var pwd = "Salakala123!";
        return $"Server=127.0.0.1,1433;Database=Master;User Id={user};Password={pwd}";
    }

    internal static string GetInvalidConnectionString()
    {
        var user = "SA";
        var pwd = "WrongPassWord";
        return $"Server=127.0.0.1,1433;Database=Master;User Id={user};Password={pwd}";
    }

    // Simple select query
    internal static int GetRowCount(string tableName)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        var getRows = connection.CreateCommand();
        getRows.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        var count = (int)getRows.ExecuteScalar();
        connection.Close();
        connection.Dispose();
        return count;
    }

    internal static void ExecuteNonQuery(string command)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = command;
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }
}
