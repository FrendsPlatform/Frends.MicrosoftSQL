using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ExceptionUnitTests : ExecuteQueryTestBase
{
    private Input input = new();
    private Options options = new();

    [TestInitialize]
    public void SetUp()
    {
        input = new Input()
        {
            ConnectionString = _connString,
        };

        options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };
    }

    [TestMethod]
    public async Task TestExecuteQuery_Invalid_Creds_ThrowError()
    {
        input.ConnectionString = Helper.GetInvalidConnectionString();

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.ExecuteQuery(input, options, default));
        Assert.IsTrue(ex.Message.Contains("Login failed for user 'SA'."));
    }

    [TestMethod]
    public async Task TestExecuteQuery_Invalid_Creds_ReturnErrorMessage()
    {
        options.ThrowErrorOnFailure = false;
        input.ConnectionString = Helper.GetInvalidConnectionString();

        var result = await MicrosoftSQL.ExecuteQuery(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."));
        Assert.AreEqual(0, result.RecordsAffected);
    }

    [TestMethod]
    public void TestExecuteQuery_ExceptionIsThrownWhenQueryFails()
    {
        input.Query = $"INSERT INTO {_tableName} VALUES (1, Unit, Tests, 456)";
        input.ExecuteType = ExecuteTypes.NonQuery;

        var ex = Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.ExecuteQuery(input, options, default));
        Assert.IsTrue(ex.Result.Message.Contains("System.Data.SqlClient.SqlException (0x80131904): Invalid column name 'Unit'."));
    }

    [TestMethod]
    public async Task TestExecuteQuery_ErrorMessageWhenQueryFails()
    {
        input.Query = $"INSERT INTO {_tableName} VALUES (1, Unit, Tests, 456)";
        input.ExecuteType = ExecuteTypes.NonQuery;

        options.ThrowErrorOnFailure = false;

        var result = await MicrosoftSQL.ExecuteQuery(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("System.Data.SqlClient.SqlException (0x80131904): Invalid column name 'Unit'."));
    }
}