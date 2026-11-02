using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

[TestClass]
public class ErrorHandlerTests
{
    [TestMethod]
    public async Task BatchOperation_Returns_Error_Details_And_Custom_Message()
    {
        var input = new Input
        {
            ConnectionString = "not a valid connection string",
        };
        var options = new Options
        {
            ThrowErrorOnFailure = false,
            ErrorMessageOnFailure = "Batch operation could not connect to SQL Server",
        };

        var result = await MicrosoftSQL.BatchOperation(input, options, default);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsNotNull(result.Error.AdditionalInfo);
        StringAssert.StartsWith(result.Error.Message, options.ErrorMessageOnFailure);
        Assert.AreEqual(result.Error.Message, result.ErrorMessage);
    }
}
