using Microsoft.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;

internal class Helper
{
    /// <summary>
    /// Test credentials for docker server.
    /// </summary>
    private static readonly string _dockerAddress = "127.0.0.1,1433";
    private static readonly string _dockerUsername = "SA";
    private static readonly string _dockerPassword = "Salakala123!";

    internal static string CreateConnectionString()
    {
        return $"Server={_dockerAddress};Database=Master;User Id={_dockerUsername};Password={_dockerPassword};Encrypt=true;TrustServerCertificate=True;";
    }

    internal static string GetInvalidConnectionString()
    {
        return $"Server=127.0.0.1,1433;Database=Master;User Id={_dockerUsername};Password={Guid.NewGuid()};Encrypt=true;TrustServerCertificate=True;";
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