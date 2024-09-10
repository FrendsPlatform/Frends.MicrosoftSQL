using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class UnitTests
{
    /*
        docker-compose up

        How to use via terminal:
        docker exec -it sql1 "bash"
        /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
        SELECT * FROM TestTable
        GO
   */

    private static readonly string _connString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!;TrustServerCertificate=True";
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
                    ""Firstname"": ""F�rst"",
                    ""Lastname"": ""L��st""
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

    [TestMethod]
    public async Task TestBulkInsert_FireTriggers()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
            SqlTransactionIsolationLevel.Default,
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = true,
                KeepIdentity = false,
                NotifyAfter = 0,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = false,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_KeepIdentity()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = true,
                NotifyAfter = 0,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = false,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_ConvertEmptyPropertyValuesToNull()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = false,
                NotifyAfter = 0,
                ConvertEmptyPropertyValuesToNull = true,
                KeepNulls = false,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_KeepNulls()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = false,
                NotifyAfter = 0,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = true,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_TableLock()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = false,
                NotifyAfter = 0,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = false,
                TableLock = true,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_All()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
            SqlTransactionIsolationLevel.Snapshot
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = true,
                KeepIdentity = true,
                NotifyAfter = 0,
                ConvertEmptyPropertyValuesToNull = true,
                KeepNulls = true,
                TableLock = true,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_NotifyAfterZero()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = false,
                NotifyAfter = -1,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = true,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_NotifyAfterTooMuch()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = false,
                NotifyAfter = 4,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = true,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_NotifyAfterOne()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        foreach (var transactionLevel in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = transactionLevel,
                CommandTimeoutSeconds = 60,
                FireTriggers = false,
                KeepIdentity = false,
                NotifyAfter = 1,
                ConvertEmptyPropertyValuesToNull = false,
                KeepNulls = true,
                TableLock = false,
            };

            var result = await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await MicrosoftSQL.BulkInsert(_input, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

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