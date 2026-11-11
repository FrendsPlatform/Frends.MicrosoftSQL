using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

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

    [TestMethod]
    public async Task Windows_InvalidUsernameFormat_ThrowsArgumentException()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("This test runs only on Windows platforms.");

        var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            TaskRunner.BatchOperation(
                new Input(),
                new Options { ThrowErrorOnFailure = true },
                default,
                CreateConnection("username-without-domain")));

        StringAssert.Contains(ex.Message, @"must be of format domain\username");
    }

    [TestMethod]
    public async Task RemoteCredentials_RequireIntegratedSecurity()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("Connection-string validation runs after the platform check on non-Windows platforms.");

        var result = await TaskRunner.BatchOperation(
            new Input(),
            new Options { ThrowErrorOnFailure = false },
            default,
            new Connection
            {
                ConnectionString = "Server=server-not-used;Database=Master;User Id=user;Password=pwd;",
                UseGivenUserCredentialsForRemoteConnections = true,
                Username = @"domain\username",
                Password = "password",
            });

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Integrated Security=true");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(ArgumentException));
    }

    [TestMethod]
    public async Task MissingCredentials_ReturnsValidationError()
    {
        var result = await TaskRunner.BatchOperation(
            new Input(),
            new Options { ThrowErrorOnFailure = false },
            default,
            new Connection { UseGivenUserCredentialsForRemoteConnections = true });

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Username is required");
        StringAssert.Contains(result.Error.Message, "ConnectionString");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(ValidationException));
    }

    [TestMethod]
    public async Task RemoteCredentialsOnLinux_ReturnsPlatformError()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("This test runs only on non-Windows platforms.");

        var result = await TaskRunner.BatchOperation(
            new Input(),
            new Options { ThrowErrorOnFailure = false },
            default,
            new Connection
            {
                ConnectionString = "Server=server-not-used;Database=Master;Integrated Security=true;",
                UseGivenUserCredentialsForRemoteConnections = true,
                Username = @"domain\username",
                Password = "password",
            });

        Assert.IsFalse(result.Success);
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(PlatformNotSupportedException));
    }

    [TestMethod]
    public async Task RemoteCredentialsOnLinux_ThrowsPlatformNotSupportedException()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("This test runs only on non-Windows platforms.");

        var ex = await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(() =>
            TaskRunner.BatchOperation(
                new Input(),
                new Options { ThrowErrorOnFailure = true },
                default,
                CreateConnection(@"domain\username")));

        Assert.AreEqual("Simple impersonation feature is only supported on Windows machines.", ex.Message);
    }

    [TestMethod]
    public async Task Windows_QueryIsExecutedAsGivenUser()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("This test runs only on Windows platforms.");

        var (server, username, password) = RequireWindowsAuthSettings();
        var escapedUsername = username.Replace("'", "''");
        var result = await TaskRunner.BatchOperation(
            new Input
            {
                Query = $"IF SUSER_SNAME() <> N'{escapedUsername}' THROW 51000, 'Unexpected SQL user', 1;",
                InputJson = "[{}]",
            },
            new Options
            {
                ThrowErrorOnFailure = false,
                SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None
            },
            default,
            CreateConnection(server, username, password));

        Assert.IsTrue(result.Success, result.Error?.Message);
    }

    [TestMethod]
    public async Task Windows_WrongPassword_FailsToLogin()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("This test runs only on Windows platforms.");

        var (server, username, _) = RequireWindowsAuthSettings();
        var result = await TaskRunner.BatchOperation(
            new Input
            {
                Query = "SELECT 1",
                InputJson = "[]"
            },
            new Options { ThrowErrorOnFailure = false },
            default,
            CreateConnection(server, username, Guid.NewGuid().ToString("N")));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Login failed");
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

    private static Connection CreateConnection(string username) => new()
    {
        ConnectionString = "Server=server-not-used;Database=Master;Integrated Security=true;",
        UseGivenUserCredentialsForRemoteConnections = true,
        Username = username,
        Password = "password",
    };

    private static Connection CreateConnection(string server, string username, string password) => new()
    {
        ConnectionString =
            $"Server={server};Database=Master;Integrated Security=true;Encrypt=true;TrustServerCertificate=True;",
        UseGivenUserCredentialsForRemoteConnections = true,
        Username = username,
        Password = password,
    };
}
