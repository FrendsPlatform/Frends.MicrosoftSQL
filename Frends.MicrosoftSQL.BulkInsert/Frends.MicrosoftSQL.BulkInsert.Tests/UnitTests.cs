using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Frends.MicrosoftSQL.BulkInsert.Tests.Lib;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class UnitTests
{
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
        TableName = TableName,
        InputData = Json,
    };

    private static string ConnString => SqlServerContainer.ConnectionString;

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

    [TestMethod]
    public async Task TestBulkInsert_FireTriggers()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_KeepIdentity()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_ConvertEmptyPropertyValuesToNull()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_KeepNulls()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_TableLock()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_All()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
            SqlTransactionIsolationLevel.Snapshot,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_NotifyAfterZero()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_NotifyAfterTooMuch()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_NotifyAfterOne()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
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

            var result = await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, GetRowCount());

            await TaskRunner.BulkInsert(defaultInput, options, default);
            Assert.AreEqual(6, GetRowCount());

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestBulkInsert_ColumnMapping()
    {
        var columnMappings = new List<ColumnMapping>()
        {
            ColumnMapping.JsonPropertyOrder,
            ColumnMapping.JsonPropertyNames,
            ColumnMapping.ManualColumnMapping,
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 0,
            ConvertEmptyPropertyValuesToNull = true,
            KeepNulls = false,
            TableLock = false,
        };

        var json = @"[
                  {
                    ""Id"": 1,
                    ""FirstName"": ""Etu"",
                    ""LastName"": ""Suku""
                  },
                  {
                    ""Id"": 2,
                    ""LastName"": ""Suku"",
                    ""FirstName"": ""Etu""
                  },
                  {
                    ""FirstName"": ""First"",
                    ""LastName"": ""Last"",
                    ""Id"": 3
                  }
                ]";

        var manualMapping = @"{ ""Id"":""Id"", ""FirstName"": ""FirstName"", ""LastName"": ""LastName"" }";

        foreach (var columnMapping in columnMappings)
        {
            var input = new Input
            {
                TableName = TableName,
                InputData = json,
                ColumnMapping = columnMapping,
                ManualColumnMapping = manualMapping,
            };

            var result = await TaskRunner.BulkInsert(input, options, default);
            Assert.IsTrue(result.Success);
        }
    }

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
