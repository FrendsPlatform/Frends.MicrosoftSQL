using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

[TestClass]
public class ExceptionUnitTests
{
    /*
        docker-compose up
   */

    [TestMethod]
    public async Task TestBatchOperation_Invalid_Creds_ThrowError()
    {
        var input = new Input()
        {
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord;TrustServerCertificate=True",
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.BatchOperation(input, options, default));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsTrue(ex.InnerException.Message.Contains("Login failed for user 'SA'."));
    }

    [TestMethod]
    public async Task TestBatchOperation_Invalid_Creds_ReturnErrorMessage()
    {
        var input = new Input()
        {
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord;TrustServerCertificate=True",
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false
        };

        var result = await MicrosoftSQL.BatchOperation(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."));
        Assert.AreEqual(0, result.RecordsAffected);
    }
}