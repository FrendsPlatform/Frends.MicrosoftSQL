using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;

internal class Helper
{
    internal static string CreateConnectionString()
    {
        return "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!;Encrypt=true;TrustServerCertificate=True;";
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