using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ManualTesting
{
    /*
        These tests requires code editing so they must be skipped in workflow.

        docker-compose up

        How to use via terminal:
        docker exec -it sql1 "bash"
        /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
        SELECT * FROM TestTable
        GO
   */

    private static readonly string _connString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!";
    private static readonly string _tableName = "TestTable";

    [TestInitialize]
    public void Init()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN CREATE TABLE {_tableName} ( Id int, LastName varchar(255), FirstName varchar(255) ); END";
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }

    [TestCleanup]
    public void CleanUp()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN DROP TABLE IF EXISTS {_tableName}; END";
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }

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
        Assert.AreEqual(0, GetRowCount());
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

    // Simple select statement for result double checks.
    private static int GetRowCount()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var getRows = connection.CreateCommand();
        getRows.CommandText = $"SELECT COUNT(*) FROM {_tableName}";
        var count = (int)getRows.ExecuteScalar();
        connection.Close();
        connection.Dispose();
        return count;
    }
}