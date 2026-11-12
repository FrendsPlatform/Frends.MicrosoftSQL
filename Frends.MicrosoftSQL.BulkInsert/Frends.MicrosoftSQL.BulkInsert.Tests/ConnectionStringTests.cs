using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class ConnectionStringTests
{
    [TestMethod]
    public void GetConnectionString_WithoutRemoteCredentials_ReturnsOriginalConnectionString()
    {
        const string connectionString = "not-a-parsed-connection-string";
        var connection = new Connection
        {
            ConnectionString = connectionString,
            UseGivenUserCredentialsForRemoteConnections = false,
        };

        var result = MicrosoftSQL.GetConnectionString(connection);

        Assert.AreEqual(connectionString, result);
    }

    [TestMethod]
    public void GetConnectionString_WithRemoteCredentials_DisablesPooling()
    {
        var connection = new Connection
        {
            ConnectionString = "Server=server;Database=Master;Integrated Security=true;Pooling=true;",
            UseGivenUserCredentialsForRemoteConnections = true,
        };

        var result = MicrosoftSQL.GetConnectionString(connection);
        var builder = new SqlConnectionStringBuilder(result);

        Assert.IsTrue(builder.IntegratedSecurity);
        Assert.IsFalse(builder.Pooling);
        Assert.AreEqual("server", builder.DataSource);
        Assert.AreEqual("Master", builder.InitialCatalog);
    }

    [TestMethod]
    public void GetConnectionString_WithRemoteCredentialsAndSqlAuthentication_ThrowsArgumentException()
    {
        var connection = new Connection
        {
            ConnectionString = "Server=server;Database=Master;User Id=user;Password=password;",
            UseGivenUserCredentialsForRemoteConnections = true,
        };

        var exception = Assert.ThrowsException<ArgumentException>(
            () => MicrosoftSQL.GetConnectionString(connection));

        StringAssert.Contains(exception.Message, "Integrated Security=true");
    }
}
