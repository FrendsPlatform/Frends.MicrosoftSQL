using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class RemoteCredentialsTests
{
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
                ConnectionString = "not-used",
                UseGivenUserCredentialsForRemoteConnections = true,
                Username = @"domain\username",
                Password = "password",
            });

        Assert.IsFalse(result.Success);
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(PlatformNotSupportedException));
    }
}
