using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

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

        await MicrosoftSQL.ExecuteQuery(input, options, default);
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

    #region Rollback
    /*
    // Rollback test. Commented because this requires manual modification and can't be ran on workflow.
    // Add following line 'throw new Exception();' under 'dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);'
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
        Assert.IsTrue(insert.ErrorMessage.Contains("ExecuteHandler exception: (If required) transaction rollback completed without exception."));
        Assert.AreEqual(0, GetRowCount());
    }

    // Rollback test. Commented because this requires manual modification and can't be ran on workflow.
    // Add following line 'throw new Exception();' under 'dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);'
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
        Assert.IsTrue(insert.Message.Contains("ExecuteHandler exception: (If required) transaction rollback completed without exception."));
    }
    */
    #endregion Rollback

    // Test all GRUD operations with different ExecuteTypes...
    #region ExecuteTypes.ExecuteReader
    // SqlTransactionIsolationLevel.Unspecified
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_Unspecified()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
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
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.None
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_None()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Default
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_Default()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Default,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadCommitted
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_ReadCommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadUncommitted
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_ReadUncommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.RepeatableRead
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_RepeatableRead()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Serializable
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_Serializable()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Snapshot
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader_Snapshot()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }
    #endregion ExecuteTypes.ExecuteReader

    #region ExecuteTypes.Auto
    // SqlTransactionIsolationLevel.Unspecified
    [TestMethod]
    public async Task TestExecuteQuery_Auto_Unspecified()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Unspecified,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.None
    [TestMethod]
    public async Task TestExecuteQuery_Auto_None()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Default
    [TestMethod]
    public async Task TestExecuteQuery_Auto_Default()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Default,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadCommitted
    [TestMethod]
    public async Task TestExecuteQuery_Auto_ReadCommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadUncommitted
    [TestMethod]
    public async Task TestExecuteQuery_Auto_ReadUncommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.RepeatableRead
    [TestMethod]
    public async Task TestExecuteQuery_Auto_RepeatableRead()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Serializable
    [TestMethod]
    public async Task TestExecuteQuery_Auto_Serializable()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Snapshot
    [TestMethod]
    public async Task TestExecuteQuery_Auto_Snapshot()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(typeof(JArray), select.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)select.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)select.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Last", (string)select.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)select.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)select.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)select.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(typeof(JArray), selectSingle.QueryResult.GetType());
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)selectSingle.QueryResult[0]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }
    #endregion ExecuteTypes.Auto

    #region ExecuteTypes.NonQuery
    // SqlTransactionIsolationLevel.Unspecified
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_Unspecified()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
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
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.None
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_None()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Default
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_Default()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Default,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadCommitted
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_ReadCommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadUncommitted
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_ReadUncommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.RepeatableRead
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_RepeatableRead()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Serializable
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_Serializable()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Snapshot
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery_Snapshot()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.QueryResult["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }
    #endregion ExecuteTypes.NonQuery

    #region ExecuteTypes.Scalar
    // SqlTransactionIsolationLevel.Unspecified
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_Unspecified()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
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
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.None
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_None()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Default
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_Default()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Default,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadCommitted
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_ReadCommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadUncommitted
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_ReadUncommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadUncommitted,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.RepeatableRead
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_RepeatableRead()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.RepeatableRead,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Serializable
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_Serializable()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Serializable,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Snapshot
    [TestMethod]
    public async Task TestExecuteQuery_Scalar_Snapshot()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Query = $@"INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Query = $"select LastName from {_tableName} where Id = 1",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Query = $@"update {_tableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Query = $"delete from {_tableName} where Id = 2",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Query = $"select * from {_tableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Snapshot,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all - Returns 1 because first value (row/column) is ID = 1
        var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(1, (int)select.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual("Suku", (string)selectSingle.QueryResult["Value"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.QueryResult[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.QueryResult[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.QueryResult[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.QueryResult[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.QueryResult[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.QueryResult[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.QueryResult[1]["FirstName"]);
    }
    #endregion ExecuteTypes.Scalar

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