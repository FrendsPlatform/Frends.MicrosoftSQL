using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class RemoteCredentialsTests
{
    private const string ServerVariable = "MSSQL_WINDOWS_AUTH_SERVER";
    private const string UsernameVariable = "MSSQL_WINDOWS_USERNAME";
    private const string PasswordVariable = "MSSQL_WINDOWS_PASSWORD";

    [TestMethod]
    public async Task Windows_InvalidUsernameFormat_ThrowsArgumentException()
    {
        RequireOs(windows: true);

        var connection = CreateConnection("server-not-used", "username-without-domain", "pwd");
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            TaskRunner.BulkInsert(
                new Input { InputData = "[]" },
                options,
                default,
                connection));

        StringAssert.Contains(ex.Message, @"must be of format domain\username");
    }

    [TestMethod]
    public async Task RemoteCredentials_RequireIntegratedSecurity()
    {
        var result = await TaskRunner.BulkInsert(
            new Input { InputData = "[]" },
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
        var result = await TaskRunner.BulkInsert(
            new Input { InputData = "[]" },
            new Options { ThrowErrorOnFailure = false },
            default,
            new Connection { UseGivenUserCredentialsForRemoteConnections = true });

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Username is required");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(ValidationException));
    }

    [TestMethod]
    public async Task RemoteCredentialsOnLinux_ReturnsPlatformError()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("This test runs only on non-Windows platforms.");

        var result = await TaskRunner.BulkInsert(
            new Input { InputData = "[]" },
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
        RequireOs(windows: false);

        var ex = await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(() =>
            TaskRunner.BulkInsert(
                new Input { InputData = "[]" },
                new Options { ThrowErrorOnFailure = true },
                default,
                CreateConnection("server-not-used", @"domain\username", "password")));

        Assert.AreEqual("Simple impersonation feature is only supported on Windows machines.", ex.Message);
    }

    [TestMethod]
    public async Task Windows_WrongPassword_FailsToLogin()
    {
        RequireOs(windows: true);
        var (server, username, _) = RequireWindowsAuthSettings();

        var result = await TaskRunner.BulkInsert(
            new Input { TableName = "MissingTable", InputData = """[{"Id":1}]""" },
            new Options { ThrowErrorOnFailure = false },
            default,
            CreateConnection(server, username, Guid.NewGuid().ToString("N")));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Login failed");
    }

    [TestMethod]
    public async Task Windows_ValidCredentialsReachSqlServer()
    {
        RequireOs(windows: true);
        var (server, username, password) = RequireWindowsAuthSettings();

        var result = await TaskRunner.BulkInsert(
            new Input
            {
                TableName = $"dbo.MissingTable_{Guid.NewGuid():N}",
                InputData = """[{"Id":1}]""",
            },
            new Options { ThrowErrorOnFailure = false },
            default,
            CreateConnection(server, username, password));

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.Error.Message.Contains("Login failed", StringComparison.OrdinalIgnoreCase));
    }

    private static Connection CreateConnection(string server, string username, string password) => new()
    {
        ConnectionString = $"Server={server};Database=Master;Integrated Security=true;Encrypt=true;TrustServerCertificate=True;",
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
