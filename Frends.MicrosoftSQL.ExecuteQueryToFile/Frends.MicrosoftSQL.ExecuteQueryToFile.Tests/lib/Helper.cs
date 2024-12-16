namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

using System.Data;
using Microsoft.Data.SqlClient;

internal static class Helper
{
    internal static string GetConnectionString()
    {
        var user = "SA";
        var pwd = "Salakala123!";
        return $"Server=127.0.0.1,1433;Database=Master;User Id={user};Password={pwd};TrustServerCertificate=True";
    }

    internal static void CreateTestTable(string connString, string tableName, string query = null)
    {
        using var connection = new SqlConnection(connString);
        connection.Open();
        var createTable = connection.CreateCommand();

        if (query == null)
            createTable.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{tableName}') BEGIN CREATE TABLE {tableName} ( Id int, LastName varchar(255), FirstName varchar(255), Salary decimal(6,2), Image Image, TestText VarBinary(MAX), TestNull Varchar(255)); END";
        else
            createTable.CommandText = query;

        createTable.ExecuteNonQuery();
        connection.Close();
    }

    internal static void ExecuteNonQuery(string connString, string commandText, SqlParameter[] parameters = null)
    {
        using var sqlConnection = new SqlConnection(connString);
        sqlConnection.Open();

        using (var command = new SqlCommand())
        {
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.Connection = sqlConnection;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
            }

            command.ExecuteNonQuery();
        }

        sqlConnection.Close();
    }
}
