using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class ExceptionUnitTests
{
    /*
         docker-compose up

         How to use via terminal:
         docker exec -it sql1 "bash"
         /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
         SELECT * FROM TestTable
         GO
    */

    private static readonly string _connString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord;TrustServerCertificate=True";
    private static readonly string _tableName = "TestTable";
    private static readonly string _json = @"[
                  {
                    ""Id"": 1,
                    ""Firstname"": ""Etu"",
                    ""Lastname"": ""Suku""
                  }
                ]";

    [TestMethod]
    public async Task TestBulkInsert_Invalid_Creds()
    {
        var input = new Input()
        {
            ConnectionString = _connString,
            TableName = _tableName,
            InputData = _json
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            ThrowErrorOnFailure = true
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.BulkInsert(input, options, default));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsTrue(ex.InnerException.Message.Contains("Login failed for user 'SA'."));
    }

    [TestMethod]
    public async Task TestBulkInsert_Invalid_Creds_ReturnErrorMessage()
    {
        var input = new Input()
        {
            ConnectionString = _connString,
            TableName = _tableName,
            InputData = _json
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false
        };

        var result = await MicrosoftSQL.BulkInsert(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."));
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task TestBulkInsert_ExecuteHandler_Exception()
    {
        var input = new Input
        {
            InputData = _json,
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!;TrustServerCertificate=True",
            TableName = "InvalidTable"
        };

        var options = new Options
        {
            ConvertEmptyPropertyValuesToNull = false,
            ThrowErrorOnFailure = true,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 60,
            NotifyAfter = 1
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.BulkInsert(input, options, CancellationToken.None));

        Assert.IsNotNull(ex.InnerException);
        Assert.IsTrue(ex.InnerException.Message.Contains("BulkInsert exception: 'Options.SqlTransactionIsolationLevel = None', so there was no transaction rollback."));
    }
}