using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Frends.MicrosoftSQL.ExecuteProcedure.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class ErrorHandlerTest
{
    [TestMethod]
    public void ShouldThrowOriginalErrorWhenThrowErrorOnFailureIsTrue()
    {
        var exception = new InvalidOperationException("Procedure execution failed.");
        var options = new Options { ThrowErrorOnFailure = true };

        var thrownException = Assert.ThrowsException<InvalidOperationException>(() => exception.Handle(options));

        Assert.AreSame(exception, thrownException);
    }

    [TestMethod]
    public void ShouldReturnErrorDetailsWhenThrowErrorOnFailureIsFalse()
    {
        var exception = new InvalidOperationException("Procedure execution failed.");
        var options = new Options { ThrowErrorOnFailure = false };

        var result = exception.Handle(options);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(exception.Message, result.Error.Message);
        Assert.AreSame(exception, result.Error.AdditionalInfo);
        Assert.AreEqual(result.Error.Message, result.ErrorMessage);
    }

    [TestMethod]
    public void ShouldIncludeCustomMessageWhenThrowErrorOnFailureIsFalse()
    {
        var exception = new InvalidOperationException("Procedure execution failed.");
        var options = new Options
        {
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = "Stored procedure failed",
        };

        var result = exception.Handle(options);

        Assert.AreEqual("Stored procedure failed: Procedure execution failed.", result.Error.Message);
    }
}
