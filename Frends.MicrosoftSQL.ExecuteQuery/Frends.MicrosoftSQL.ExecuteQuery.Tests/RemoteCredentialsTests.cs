using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

/// <summary>
/// Tests for Connection.UseGivenUserCredentialsForRemoteConnections.
/// Tests that need a real Windows-authenticated SQL Server read their settings from environment variables:
/// MSSQL_WINDOWS_AUTH_SERVER (e.g. "myserver,1433"), MSSQL_WINDOWS_USERNAME (domain\username)
/// and MSSQL_WINDOWS_PASSWORD. They are reported as inconclusive when the variables are not set.
/// </summary>
[TestClass]
public class RemoteCredentialsTests
{
    private const string ServerVariable = "MSSQL_WINDOWS_AUTH_SERVER";
    private const string UsernameVariable = "MSSQL_WINDOWS_USERNAME";
    private const string PasswordVariable = "MSSQL_WINDOWS_PASSWORD";

    private static readonly Input WhoAmIInput = new()
    {
        Query = "SELECT SUSER_SNAME()",
        ExecuteType = ExecuteTypes.Scalar,
        Parameters = null,
    };

    private static readonly Options NoThrowOptions = new()
    {
        SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
        CommandTimeoutSeconds = 30,
        ThrowErrorOnFailure = false,
    };

    [TestMethod]
    public async Task Windows_InvalidUsernameFormat_ThrowsArgumentException()
    {
        RequireOs(windows: true);

        var connection = CreateConnection("server-not-used", "username-without-domain", "pwd");
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, options, default));

        StringAssert.Contains(ex.Message, @"must be of format domain\username");
        StringAssert.Contains(ex.Message, "username-without-domain");
    }

    [TestMethod]
    public async Task Windows_MissingUsername_ReturnsErrorResult()
    {
        RequireOs(windows: true);

        var connection = CreateConnection("server-not-used", string.Empty, "pwd");

        var result = await MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, NoThrowOptions, default);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Username is required");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(ValidationException));
    }

    [TestMethod]
    public async Task RemoteCredentials_RequireIntegratedSecurity()
    {
        var connection = new Connection
        {
            ConnectionString = "Server=server-not-used;Database=Master;User Id=user;Password=pwd;",
            UseGivenUserCredentialsForRemoteConnections = true,
            Username = @"domain\username",
            Password = "password",
        };

        var result = await MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, NoThrowOptions, default);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Integrated Security=true");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(ArgumentException));
    }

    [TestMethod]
    public async Task Windows_UnsupportedPlatformErrorIsNotRaised()
    {
        RequireOs(windows: true);

        // A malformed username fails on validation, which is only reached on Windows after the platform check.
        var connection = CreateConnection("server-not-used", "invalid", "pwd");

        var result = await MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, NoThrowOptions, default);

        Assert.IsFalse(result.Success);
        Assert.IsNotInstanceOfType(result.Error.AdditionalInfo, typeof(PlatformNotSupportedException));
    }

    // This test requires a real Windows-authenticated SQL Server, hence it won't run in our CI/CD pipeline.
    [TestMethod]
    public async Task Windows_QueryIsExecutedAsGivenUser()
    {
        RequireOs(windows: true);
        var (server, username, password) = RequireWindowsAuthSettings();

        var connection = CreateConnection(server, username, password);

        var result = await MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, NoThrowOptions, default);

        Assert.IsTrue(result.Success, result.Error?.Message);
        Assert.AreEqual(1, result.RecordsAffected);
        Assert.AreEqual(username, (string)result.Data["Value"], ignoreCase: true);
    }

    // This test requires a real Windows-authenticated SQL Server, hence it won't run in our CI/CD pipeline.
    [TestMethod]
    public async Task Windows_WrongPassword_FailsToLogin()
    {
        RequireOs(windows: true);
        var (server, username, _) = RequireWindowsAuthSettings();

        var connection = CreateConnection(server, username, Guid.NewGuid().ToString("N"));

        var result = await MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, NoThrowOptions, default);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Login failed");
    }

    [TestMethod]
    public async Task Linux_UseGivenUserCredentials_ThrowsPlatformNotSupportedException()
    {
        RequireOs(windows: false);

        var connection = CreateConnection("server-not-used", @"domain\username", "pwd");
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(() =>
            MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, options, default));

        Assert.AreEqual("Simple impersonation feature is only supported on Windows machines.", ex.Message);
    }

    [TestMethod]
    public async Task Linux_UseGivenUserCredentials_ReturnsErrorResultWhenNotThrowing()
    {
        RequireOs(windows: false);

        var connection = CreateConnection("server-not-used", @"domain\username", "pwd");

        var result = await MicrosoftSQL.ExecuteQuery(WhoAmIInput, connection, NoThrowOptions, default);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Simple impersonation feature is only supported on Windows machines.", result.Error.Message);
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(PlatformNotSupportedException));
    }

    private static Connection CreateConnection(string server, string username, string password) => new()
    {
        ConnectionString =
            $"Server={server};Database=Master;Integrated Security=true;Encrypt=true;TrustServerCertificate=True;",
        UseGivenUserCredentialsForRemoteConnections = true,
        Username = username,
        Password = password,
    };

    private static void RequireOs(bool windows)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) != windows)
            Assert.Inconclusive($"This test runs only on {(windows ? "Windows" : "non-Windows")} platforms.");
    }

    private static (string Server, string Username, string Password) RequireWindowsAuthSettings()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        var username = Environment.GetEnvironmentVariable(UsernameVariable);
        var password = Environment.GetEnvironmentVariable(PasswordVariable);

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            Assert.Inconclusive($"Set {ServerVariable}, {UsernameVariable} and {PasswordVariable} to run this test.");

        return (server, username, password);
    }
}
