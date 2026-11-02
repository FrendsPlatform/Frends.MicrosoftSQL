using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests.HelpersTests;

[TestClass]
public class ErrorHandlerTest
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [TestMethod]
    public async Task Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var options = new Options { ThrowErrorOnFailure = true };
        await Assert.ThrowsExceptionAsync<Newtonsoft.Json.JsonReaderException>(() => MicrosoftSQL.BulkInsert(InvalidInput(), options, default));
    }

    [TestMethod]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = new Options { ThrowErrorOnFailure = false };
        var result = await MicrosoftSQL.BulkInsert(InvalidInput(), options, default);
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = new Options { ThrowErrorOnFailure = true, ErrorMessageOnFailure = CustomErrorMessage };
        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.BulkInsert(InvalidInput(), options, default));
        Assert.IsTrue(ex.Message.Contains(CustomErrorMessage));
    }

    private static Input InvalidInput() => new()
    {
        ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;******;TrustServerCertificate=True",
        TableName = "TestTable",
        InputData = "not a valid json",
    };
}
