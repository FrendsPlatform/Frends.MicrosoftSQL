namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

using System;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Helpers;
using NUnit.Framework;

[TestFixture]
public class ErrorHandlerTests
{
    [Test]
    public async Task ExecuteQueryToFile_ReturnsFailedResult_WhenThrowErrorOnFailureIsFalse()
    {
        var options = new Options { ThrowErrorOnFailure = false };

        var result = await MicrosoftSQL.ExecuteQueryToFile(new Input(), null, options, default);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<NullReferenceException>());
    }

    [Test]
    public void Handle_ReturnsFailedResult_WhenThrowErrorOnFailureIsFalse()
    {
        var exception = new InvalidOperationException("Database unavailable.");
        var options = new Options
        {
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = "Query export failed.",
        };

        var result = exception.Handle(options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Is.EqualTo("Query export failed.: Database unavailable."));
        Assert.That(result.Error.AdditionalInfo, Is.SameAs(exception));
    }

    [Test]
    public void Handle_RethrowsOriginalException_WhenThrowErrorOnFailureIsTrueWithoutCustomMessage()
    {
        var exception = new InvalidOperationException("Database unavailable.");
        var options = new Options { ThrowErrorOnFailure = true };

        var thrownException = Assert.Throws<InvalidOperationException>(() => exception.Handle(options));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void Handle_WrapsException_WhenCustomMessageIsProvided()
    {
        var exception = new InvalidOperationException("Database unavailable.");
        var options = new Options
        {
            ThrowErrorOnFailure = true,
            ErrorMessageOnFailure = "Query export failed.",
        };

        var thrownException = Assert.Throws<Exception>(() => exception.Handle(options));

        Assert.That(thrownException.Message, Is.EqualTo("Query export failed."));
        Assert.That(thrownException.InnerException, Is.SameAs(exception));
    }

    [Test]
    public void Handle_RethrowsCancellation_WhenThrowErrorOnFailureIsFalse()
    {
        var exception = new OperationCanceledException();
        var options = new Options { ThrowErrorOnFailure = false };

        var thrownException = Assert.Throws<OperationCanceledException>(() => exception.Handle(options));

        Assert.That(thrownException, Is.SameAs(exception));
    }
}
