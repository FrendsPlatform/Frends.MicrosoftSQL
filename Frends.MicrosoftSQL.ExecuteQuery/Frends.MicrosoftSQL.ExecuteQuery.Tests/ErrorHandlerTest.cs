using System;
using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ErrorHandlerTest
{
    [TestMethod]
    public void HandleReturnsErrorDetailsWhenThrowingIsDisabled()
    {
        var exception = new InvalidOperationException("Invalid SQL query.");
        var options = new Options
        {
            ThrowErrorOnFailure = false,
        };

        var result = exception.Handle(options);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid SQL query.", result.Error.Message);
        Assert.AreEqual("Invalid SQL query.", result.ErrorMessage);
        Assert.AreSame(exception, result.Error.AdditionalInfo);
    }

    [TestMethod]
    public void HandleUsesConfiguredErrorMessage()
    {
        var options = new Options
        {
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = "Query execution failed",
        };

        var result = new InvalidOperationException("Invalid SQL query.").Handle(options);

        Assert.AreEqual("Query execution failed: Invalid SQL query.", result.Error.Message);
    }

    [TestMethod]
    public void HandleThrowsConfiguredErrorMessageWhenThrowingIsEnabled()
    {
        var options = new Options
        {
            ThrowErrorOnFailure = true,
            ErrorMessageOnFailure = "Query execution failed",
        };

        var exception = Assert.ThrowsException<Exception>(() => new InvalidOperationException("Invalid SQL query.").Handle(options));

        Assert.AreEqual("Query execution failed", exception.Message);
        Assert.AreEqual("Invalid SQL query.", exception.InnerException!.Message);
    }
}
