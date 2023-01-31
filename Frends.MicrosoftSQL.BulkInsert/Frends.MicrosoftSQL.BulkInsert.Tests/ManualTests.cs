using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class ManualTests
{
    /*
        docker-compose up

        How to use via terminal:
        docker exec -it sql1 "bash"
        /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
        SELECT * FROM TestTable
        GO
   */

    private static readonly string _connString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!";
    private static readonly string _tableName = "TestTable";
    private static readonly string _json = @"[
                  {
                    ""Id"": 1,
                    ""Firstname"": ""Etu"",
                    ""Lastname"": ""Suku""
                  },
                  {
                    ""Id"": 2,
                    ""Firstname"": ""Suku"",
                    ""Lastname"": ""Etu""
                  },
                  {
                    ""Id"": 3,
                    ""Firstname"": ""Först"",
                    ""Lastname"": ""Lääst""
                  }
                ]";

    readonly Input _input = new()
    {
        ConnectionString = _connString,
        TableName = _tableName,
        InputData = _json
    };

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

    // Add following line to BulkInsert.cs: 'throw new Exception();' before 'transaction.Commit();' (currently line 78).
    [Ignore("To run this test, comment this line after exception has been added to BulkInsert.cs.")]
    [TestMethod]
    public async Task TestBulkInsert_RollbackSuccess_ThrowErrorOnFailure_False()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = false
        };


        var result = await MicrosoftSQL.BulkInsert(_input, options, default);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.Count);
        Assert.IsTrue(result.ErrorMessage.Contains("(If required) transaction rollback completed without exception."));
        Assert.AreEqual(0, GetRowCount());
    }

    // Add following line to BulkInsert.cs: 'throw new Exception();' before 'transaction.Commit();' (currently line 78).
    [Ignore("To run this test, comment this line after exception has been added to BulkInsert.cs.")]
    [TestMethod]
    public async Task TestBulkInsert_RollbackInsert_ThrowErrorOnFailure_True()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.BulkInsert(_input, options, default));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsTrue(ex.InnerException.Message.Contains("(If required) transaction rollback completed without exception."));
        Assert.Equals(0, GetRowCount());
    }

    // Simple select query
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