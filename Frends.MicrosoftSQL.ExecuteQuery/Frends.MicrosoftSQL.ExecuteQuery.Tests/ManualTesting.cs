using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ManualTesting : ExecuteQueryTestBase
{
    // Add following line to ExecuteQuery.cs: 'throw new Exception();' under 'dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);' (currently line 124)
    [Ignore("To run this test, comment this line after exception has been added to ExecuteQuery.cs.")]
    [TestMethod]
    public async Task TestExecuteQuery_RollbackInsert_ThrowErrorOnFailure_False()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsFalse(insert.Success);
        Assert.AreEqual(0, insert.RecordsAffected);
        Assert.IsTrue(insert.ErrorMessage.Contains("(If required) transaction rollback completed without exception."));
        Assert.AreEqual(0, Helper.GetRowCount(_connString, _tableName));
    }

    // Add following line to ExecuteQuery.cs: 'throw new Exception();' under 'dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);' (currently line 124)
    [Ignore("To run this test, comment this line after exception has been added to ExecuteQuery.cs.")]
    [TestMethod]
    public async Task TestExecuteQuery_RollbackInsert_ThrowErrorOnFailure_True()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.ExecuteQuery(inputInsert, options, default));
        Assert.IsTrue(insert.Message.Contains("(If required) transaction rollback completed without exception."));
    }
}