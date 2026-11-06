using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class TransactionRollbackTests : ExecuteQueryTestBase
{
    // The INSERT and SELECT succeed, then THROW raises an error while the reader advances to the next result, forcing a rollback.
    private static string FailingInsertQuery => $@"INSERT INTO {TableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki'); SELECT * FROM {TableName}; THROW 51000, 'Forced failure after insert.', 1;";

    [TestMethod]
    public async Task TestExecuteQuery_RollbackInsert_ThrowErrorOnFailure_False()
    {
        var input = new Input()
        {
            Query = FailingInsertQuery,
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false,
        };

        var result = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.RecordsAffected);
        Assert.IsTrue(result.Error.Message.Contains("(If required) transaction rollback completed without exception."));
        Assert.IsTrue(result.Error.Message.Contains("Forced failure after insert."));
        Assert.AreEqual(0, Helper.GetRowCount(ConnString, TableName));
    }

    [TestMethod]
    public async Task TestExecuteQuery_RollbackInsert_ThrowErrorOnFailure_True()
    {
        var input = new Input()
        {
            Query = FailingInsertQuery,
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true,
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default));
        Assert.IsTrue(ex.Message.Contains("(If required) transaction rollback completed without exception."));
        Assert.IsTrue(ex.Message.Contains("Forced failure after insert."));
        Assert.AreEqual(0, Helper.GetRowCount(ConnString, TableName));
    }

    [TestMethod]
    public async Task TestExecuteQuery_NoTransaction_InsertIsNotRolledBack()
    {
        var input = new Input()
        {
            Query = FailingInsertQuery,
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false,
        };

        var result = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Error.Message.Contains("there was no transaction rollback"));
        Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName));
    }
}
