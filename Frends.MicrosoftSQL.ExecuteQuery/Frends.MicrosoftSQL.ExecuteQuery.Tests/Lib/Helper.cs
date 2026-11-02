using Microsoft.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;

internal class Helper
{
    /// <summary>
    /// Test credentials for docker server.
    /// </summary>
    private static readonly string DockerAddress = "127.0.0.1,1433";
    private static readonly string DockerUsername = "SA";
    private static readonly string DockerPassword = "Salakala123!";

    internal static string CreateConnectionString()
    {
        return $"Server={DockerAddress};Database=Master;User Id={DockerUsername};Password={DockerPassword};Encrypt=true;TrustServerCertificate=True;";
    }

    internal static string GetInvalidConnectionString()
    {
        return $"Server=127.0.0.1,1433;Database=Master;User Id={DockerUsername};Password={Guid.NewGuid()};Encrypt=true;TrustServerCertificate=True;";
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