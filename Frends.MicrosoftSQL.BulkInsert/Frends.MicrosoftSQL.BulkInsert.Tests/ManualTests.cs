using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    private static readonly string ConnString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!";
    private static readonly string TableName = "TestTable";
    private static readonly string Json = @"[
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
                    ""Firstname"": ""F�rst"",
                    ""Lastname"": ""L��st""
                  }
                ]";

    private readonly Input defaultInput = new()
    {
        ConnectionString = ConnString,
        TableName = TableName,
        InputData = Json,
    };

    [TestInitialize]
    public void Init()
    {
        using var connection = new SqlConnection(ConnString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{TableName}') BEGIN CREATE TABLE {TableName} ( Id int, LastName varchar(255), FirstName varchar(255) ); END";
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }

    [TestCleanup]
    public void CleanUp()
    {
        using var connection = new SqlConnection(ConnString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{TableName}') BEGIN DROP TABLE IF EXISTS {TableName}; END";
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
            ThrowErrorOnFailure = false,
        };

        var result = await MicrosoftSQL.BulkInsert(defaultInput, options, default);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.Count);
        Assert.IsTrue(result.Error.Message.Contains("(If required) transaction rollback completed without exception."));
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
            ThrowErrorOnFailure = true,
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.BulkInsert(defaultInput, options, default));
        Assert.IsTrue(ex.Message.Contains("(If required) transaction rollback completed without exception."));
        Assert.Equals(0, GetRowCount());
    }

    // Simple select query
    private static int GetRowCount()
    {
        using var connection = new SqlConnection(ConnString);
        connection.Open();
        var getRows = connection.CreateCommand();
        getRows.CommandText = $"SELECT COUNT(*) FROM {TableName}";
        var count = (int)getRows.ExecuteScalar();
        connection.Close();
        connection.Dispose();
        return count;
    }
}
