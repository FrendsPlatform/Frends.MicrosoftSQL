using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using NUnit.Framework;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

[TestFixture]
public class RemoteCredentialsTests
{
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
                ConnectionString = "not-used",
                UseGivenUserCredentialsForRemoteConnections = true,
                Username = @"domain\username",
                Password = "password",
            },
            new Options { ThrowErrorOnFailure = false },
            default);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.InstanceOf<PlatformNotSupportedException>());
    }
}
