using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class NonQueryUnitTests
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
    
    [TestInitialize]
    public void Init()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var command = connection.CreateCommand();

        //Create
        command.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN CREATE TABLE {_tableName} ( Id int, LastName varchar(255), FirstName varchar(255) ); END";
        command.ExecuteNonQuery();

        //Insert
        command.CommandText = $@"CREATE PROCEDURE InsertValues AS INSERT INTO {_tableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')";
        command.ExecuteNonQuery();

        //Select all
        command.CommandText = $"CREATE PROCEDURE SelectAll AS select * from {_tableName}";
        command.ExecuteNonQuery();

        //Select single
        command.CommandText = $"CREATE PROCEDURE SelectSingle AS select * from {_tableName} where Id = 1";
        command.ExecuteNonQuery();

        //Select lastname
        command.CommandText = $"CREATE PROCEDURE SelectLastname AS select LastName from {_tableName} where Id = 1";
        command.ExecuteNonQuery();

        //Update
        command.CommandText = $@"CREATE PROCEDURE UpdateValue AS update {_tableName} set LastName = 'Edit' where Id = 2";
        command.ExecuteNonQuery();

        //Delete
        command.CommandText = $"CREATE PROCEDURE DeleteValue AS delete from {_tableName} where Id = 2";
        command.ExecuteNonQuery();

        connection.Close();
        connection.Dispose();
    }

    [TestCleanup]
    public void CleanUp()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN DROP TABLE IF EXISTS {_tableName}; END";
        command.ExecuteNonQuery();
        command.CommandText = $@"DECLARE @procedureName varchar(500)
DECLARE cur CURSOR
      FOR SELECT [name] FROM sys.objects WHERE type = 'p'
      OPEN cur

      FETCH NEXT FROM cur INTO @procedureName
      WHILE @@fetch_status = 0
      BEGIN
            EXEC('DROP PROCEDURE ' + @procedureName)
            FETCH NEXT FROM cur INTO @procedureName
      END
      CLOSE cur
      DEALLOCATE cur";
        command.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }

    [TestMethod]
    public async Task TestExecuteProcedure_Invalid_Creds_ThrowError()
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

        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.ExecuteProcedure(input, options, default));
        Assert.IsTrue(ex.Message.Contains("SqlException (0x80131904): Login failed for user 'SA'."));
    }

    [TestMethod]
    public async Task TestExecuteProcedure_Invalid_Creds_ReturnErrorMessage()
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

        var result = await MicrosoftSQL.ExecuteProcedure(input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."));
        Assert.AreEqual(0, result.RecordsAffected);
    }

    // SqlTransactionIsolationLevel.Unspecified
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_Unspecified()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.None
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_None()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Default
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_Default()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadCommitted
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_ReadCommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.ReadUncommitted
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_ReadUncommitted()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.RepeatableRead
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_RepeatableRead()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Serializable
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_Serializable()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
    }

    // SqlTransactionIsolationLevel.Snapshot
    [TestMethod]
    public async Task TestExecuteProcedure_NonQuery_Snapshot()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
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
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        // Insert rows
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(3, insert.RecordsAffected);
        Assert.IsNull(insert.ErrorMessage);
        Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // Make sure rows inserted before moving on.

        // Select all
        var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
        Assert.IsTrue(select.Success);
        Assert.AreEqual(-1, select.RecordsAffected);
        Assert.IsNull(select.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Select single
        var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
        Assert.IsTrue(selectSingle.Success);
        Assert.AreEqual(-1, selectSingle.RecordsAffected);
        Assert.IsNull(selectSingle.ErrorMessage);
        Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Update
        var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
        Assert.IsTrue(update.Success);
        Assert.AreEqual(1, update.RecordsAffected);
        Assert.IsNull(update.ErrorMessage);
        Assert.AreEqual(3, GetRowCount()); // double check
        var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
        Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
        Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
        Assert.AreEqual(3, GetRowCount()); // double check

        // Delete
        var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
        Assert.IsTrue(delete.Success);
        Assert.AreEqual(1, delete.RecordsAffected);
        Assert.IsNull(delete.ErrorMessage);
        Assert.AreEqual(2, GetRowCount()); // double check
        var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
        Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
        Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
        Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
        Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);
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