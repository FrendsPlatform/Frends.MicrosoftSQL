using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using NUnit.Framework;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

[TestFixture]
public class RemoteCredentialsTests
{
    private const string ServerVariable = "MSSQL_WINDOWS_AUTH_SERVER";
    private const string UsernameVariable = "MSSQL_WINDOWS_USERNAME";
    private const string PasswordVariable = "MSSQL_WINDOWS_PASSWORD";

    [Test]
    public void Windows_InvalidUsernameFormat_ThrowsArgumentException()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("This test runs only on Windows platforms.");

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await MicrosoftSQL.ExecuteQueryToFile(
                new Input(),
                CreateConnection("username-without-domain"),
                new Options { ThrowErrorOnFailure = true },
                default));

        Assert.That(ex.Message, Does.Contain(@"must be of format domain\username"));
    }

    [Test]
    public async Task RemoteCredentials_RequireIntegratedSecurity()
    {
        var result = await MicrosoftSQL.ExecuteQueryToFile(
            new Input(),
            new Connection
            {
                ConnectionString = "Server=server-not-used;Database=Master;User Id=user;Password=pwd;",
                UseGivenUserCredentialsForRemoteConnections = true,
                Username = @"domain\username",
                Password = "password",
            },
            new Options { ThrowErrorOnFailure = false },
            default);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Integrated Security=true"));
        Assert.That(result.Error.AdditionalInfo, Is.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task MissingCredentials_ReturnsValidationError()
    {
        var result = await MicrosoftSQL.ExecuteQueryToFile(
            new Input(),
            new Connection { UseGivenUserCredentialsForRemoteConnections = true },
            new Options { ThrowErrorOnFailure = false },
            default);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Username is required"));
        Assert.That(result.Error.AdditionalInfo, Is.InstanceOf<ValidationException>());
    }

    [Test]
    public async Task RemoteCredentialsOnLinux_ReturnsPlatformError()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("This test runs only on non-Windows platforms.");

        var result = await MicrosoftSQL.ExecuteQueryToFile(
            new Input(),
            new Connection
            {
                ConnectionString = "Server=server-not-used;Database=Master;Integrated Security=true;",
                UseGivenUserCredentialsForRemoteConnections = true,
                Username = @"domain\username",
                Password = "password",
            },
            new Options { ThrowErrorOnFailure = false },
            default);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.InstanceOf<PlatformNotSupportedException>());
    }

    [Test]
    public void RemoteCredentialsOnLinux_ThrowsPlatformNotSupportedException()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("This test runs only on non-Windows platforms.");

        var ex = Assert.ThrowsAsync<PlatformNotSupportedException>(async () =>
            await MicrosoftSQL.ExecuteQueryToFile(
                new Input(),
                CreateConnection(@"domain\username"),
                new Options { ThrowErrorOnFailure = true },
                default));

        Assert.That(ex.Message, Is.EqualTo("Simple impersonation feature is only supported on Windows machines."));
    }

    [Test]
    public async Task Windows_QueryIsExecutedAsGivenUser()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("This test runs only on Windows platforms.");

        var (server, username, password) = RequireWindowsAuthSettings();
        var filePath = Path.Combine(Path.GetTempPath(), $"Frends.MicrosoftSQL.{Guid.NewGuid():N}.csv");
        try
        {
            var result = await MicrosoftSQL.ExecuteQueryToFile(
                new Input { Query = "SELECT SUSER_SNAME() AS UserName", OutputFilePath = filePath },
                CreateConnection(server, username, password),
                new Options { ThrowErrorOnFailure = false },
                default);

            Assert.That(result.Success, Is.True, result.Error?.Message);
            Assert.That(File.ReadAllText(filePath), Does.Contain(username));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Test]
    public async Task Windows_WrongPassword_FailsToLogin()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("This test runs only on Windows platforms.");

        var (server, username, _) = RequireWindowsAuthSettings();
        var filePath = Path.Combine(Path.GetTempPath(), $"Frends.MicrosoftSQL.{Guid.NewGuid():N}.csv");
        try
        {
            var result = await MicrosoftSQL.ExecuteQueryToFile(
                new Input { Query = "SELECT 1", OutputFilePath = filePath },
                CreateConnection(server, username, Guid.NewGuid().ToString("N")),
                new Options { ThrowErrorOnFailure = false },
                default);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Message, Does.Contain("Login failed"));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private static (string Server, string Username, string Password) RequireWindowsAuthSettings()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        var username = Environment.GetEnvironmentVariable(UsernameVariable);
        var password = Environment.GetEnvironmentVariable(PasswordVariable);

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            Assert.Ignore($"Set {ServerVariable}, {UsernameVariable} and {PasswordVariable} to run this test.");

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
        ConnectionString = $"Server={server};Database=Master;Integrated Security=true;Encrypt=true;TrustServerCertificate=True;",
        UseGivenUserCredentialsForRemoteConnections = true,
        Username = username,
        Password = password,
    };
}
