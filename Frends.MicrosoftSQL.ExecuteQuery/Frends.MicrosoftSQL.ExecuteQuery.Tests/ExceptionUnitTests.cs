using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ExceptionUnitTests : ExecuteQueryTestBase
{
    [TestMethod]
    public async Task TestExecuteQuery_Invalid_Creds_ThrowError()
    {
        var input = new Input()
        {
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord",
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.ExecuteQuery(input, options, default));
        Assert.IsTrue(ex.Message.Contains("Login failed for user 'SA'."));
    }

    [TestMethod]
    public async Task TestExecuteQuery_Invalid_Creds_ReturnErrorMessage()
    {
        var input = new Input()
        {
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord",
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false
        };

        var result = await MicrosoftSQL.ExecuteQuery(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."));
        Assert.AreEqual(0, result.RecordsAffected);
    }

    [TestMethod]
    public void TestExecuteQuery_ExceptionIsThrownWhenQueryFails()
    {
        var input = new Input()
        {
            Query = $"INSERT INTO {_tableName} VALUES (1, Unit, Tests, 456)",
            ExecuteType = ExecuteTypes.NonQuery,
            ConnectionString = _connString,
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        var ex = Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.ExecuteQuery(input, options, default));
        Assert.IsTrue(ex.Result.Message.Contains("System.Data.SqlClient.SqlException (0x80131904): Invalid column name 'Unit'."));
    }

    [TestMethod]
    public async Task TestExecuteQuery_ErrorMessageWhenQueryFails()
    {
        var input = new Input()
        {
            Query = $"INSERT INTO {_tableName} VALUES (1, Unit, Tests, 456)",
            ExecuteType = ExecuteTypes.NonQuery,
            ConnectionString = _connString,
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false
        };

        var result = await MicrosoftSQL.ExecuteQuery(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("System.Data.SqlClient.SqlException (0x80131904): Invalid column name 'Unit'."));
    }
}