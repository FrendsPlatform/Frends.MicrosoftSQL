using System;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

[TestFixture]
public class ConnectionStringTests
{
    [Test]
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

    [Test]
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

    [Test]
    public void GetConnectionString_WithRemoteCredentialsAndSqlAuthentication_ThrowsArgumentException()
    {
        var connection = new Connection
        {
            ConnectionString = "Server=server;Database=Master;User Id=user;Password=password;",
            UseGivenUserCredentialsForRemoteConnections = true,
        };

        var exception = Assert.Throws<ArgumentException>(
            () => MicrosoftSQL.GetConnectionString(connection));

        Assert.That(exception.Message, Does.Contain("Integrated Security=true"));
    }
}
