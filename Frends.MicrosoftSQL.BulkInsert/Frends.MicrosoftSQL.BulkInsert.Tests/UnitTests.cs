using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class UnitTests
{
    /*
        docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        docker exec -it sql1 "bash"
        /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
        
        Check rows before CleanUp:
        SELECT * FROM TestTable
        GO
    
        Optional queries:
        SELECT Name FROM sys.Databases;
        GO
        SELECT * FROM INFORMATION_SCHEMA.TABLES;
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

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public async Task TestBulkInsert_Invalid_Creds()
    {
        var input = new Input()
        {
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord",
            TableName = _tableName,
            InputData = _json
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
            ConvertEmptyPropertyValuesToNull = false,
            KeepNulls = false,
            TableLock = false,
        };

        await MicrosoftSQL.BulkInsert(input, options, default);
        Assert.AreEqual(0, GetRowCount());
    }

    #region SqlTransactionIsolationLevel.ReadCommitted
    [TestMethod]
    public async Task TestBulkInsert_ReadCommitted_FireTriggers()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadCommitted_KeepIdentity()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadCommitted_ConvertEmptyPropertyValuesToNull()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadCommitted_KeepNulls()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadCommitted_TableLock()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadCommitted_All()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }
    #endregion SqlTransactionIsolationLevel.ReadCommitted

    #region SqlTransactionIsolationLevel.Unspecified
    [TestMethod]
    public async Task TestBulkInsert_Unspecified_FireTriggers()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Unspecified_KeepIdentity()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Unspecified_ConvertEmptyPropertyValuesToNull()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Unspecified_KeepNulls()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Unspecified_TableLock()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Unspecified_All()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }
    #endregion SqlTransactionIsolationLevel.Unspecified

    #region SqlTransactionIsolationLevel.ReadUncommitted
    [TestMethod]
    public async Task TestBulkInsert_ReadUncommitted_FireTriggers()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadUncommitted_KeepIdentity()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadUncommitted_ConvertEmptyPropertyValuesToNull()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadUncommitted_KeepNulls()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadUncommitted_TableLock()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_ReadUncommitted_All()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }
    #endregion SqlTransactionIsolationLevel.ReadUncommitted

    #region SqlTransactionIsolationLevel.RepeatableRead
    [TestMethod]
    public async Task TestBulkInsert_RepeatableRead_FireTriggers()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_RepeatableRead_KeepIdentity()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_RepeatableRead_ConvertEmptyPropertyValuesToNull()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_RepeatableRead_KeepNulls()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_RepeatableRead_TableLock()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_RepeatableRead_All()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }
    #endregion SqlTransactionIsolationLevel.RepeatableRead

    #region SqlTransactionIsolationLevel.Serializable
    [TestMethod]
    public async Task TestBulkInsert_Serializable_FireTriggers()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Serializable_KeepIdentity()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Serializable_ConvertEmptyPropertyValuesToNull()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Serializable_KeepNulls()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Serializable_TableLock()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Serializable_All()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }
    #endregion SqlTransactionIsolationLevel.Serializable

    #region SqlTransactionIsolationLevel.Snapshot
    [TestMethod]
    public async Task TestBulkInsert_Snapshot_FireTriggers()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Snapshot_KeepIdentity()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Snapshot_ConvertEmptyPropertyValuesToNull()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Snapshot_KeepNulls()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Snapshot_TableLock()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 60,
            FireTriggers = false,
            KeepIdentity = false,
            NotifyAfter = 1,
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
    }

    [TestMethod]
    public async Task TestBulkInsert_Snapshot_All()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 60,
            FireTriggers = true,
            KeepIdentity = true,
            NotifyAfter = 1,
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
    }
    #endregion SqlTransactionIsolationLevel.Snapshot

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