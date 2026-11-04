using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

/// <summary>
/// docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
/// with Git bash add winpty to the start of
/// winpty docker exec -it sql1 "bash"
/// /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
/// Check rows before CleanUp:
/// SELECT* FROM TestTable
/// GO
/// Optional queries:
/// SELECT Name FROM sys.Databases;
/// GO
/// SELECT* FROM INFORMATION_SCHEMA.TABLES;
/// GO
/// </summary>
[TestClass]
public class NonQueryUnitTests
{
    private static readonly string ConnectionString = Helper.GetConnectionString();
    private static readonly string TableName = "TestTable";

    [TestInitialize]
    public void Init()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();

        // Create
        command.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{TableName}') BEGIN CREATE TABLE {TableName} ( Id int, LastName varchar(255), FirstName varchar(255) ); END";
        command.ExecuteNonQuery();

        // Insert
        command.CommandText = $@"CREATE PROCEDURE InsertValues AS INSERT INTO {TableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')";
        command.ExecuteNonQuery();

        // Select all
        command.CommandText = $"CREATE PROCEDURE SelectAll AS select * from {TableName}";
        command.ExecuteNonQuery();

        // Select single parameter
        command.CommandText = $"CREATE PROCEDURE InsertWithParameter (@lastname varchar(255)) AS INSERT INTO {TableName} VALUES (4, @lastname, 'Etu')";
        command.ExecuteNonQuery();

        // Select single
        command.CommandText = $"CREATE PROCEDURE SelectSingle AS select * from {TableName} where Id = 1";
        command.ExecuteNonQuery();

        // Select lastname
        command.CommandText = $"CREATE PROCEDURE SelectLastname AS select LastName from {TableName} where Id = 1";
        command.ExecuteNonQuery();

        // Update
        command.CommandText = $@"CREATE PROCEDURE UpdateValue AS update {TableName} set LastName = 'Edit' where Id = 2";
        command.ExecuteNonQuery();

        // Delete
        command.CommandText = $"CREATE PROCEDURE DeleteValue AS delete from {TableName} where Id = 2";
        command.ExecuteNonQuery();

        connection.Close();
        connection.Dispose();
    }

    [TestCleanup]
    public void CleanUp()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{TableName}') BEGIN DROP TABLE IF EXISTS {TableName}; END";
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
    public async Task TestExecuteProcedure_NonQuery()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
        };

        var inputInsert = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputSelect = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputUpdate = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputDelete = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputSelectAfterExecution = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        foreach (var level in transactionLevels)
        {
            var options = new Options()
            {
                SqlTransactionIsolationLevel = level,
                CommandTimeoutSeconds = 2,
                ThrowErrorOnFailure = true,
            };

            // Insert rows
            var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
            Assert.IsTrue(insert.Success);
            Assert.AreEqual(3, insert.RecordsAffected);
            Assert.IsNull(insert.Error);
            Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName)); // Make sure rows inserted before moving on.

            // Select all
            var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
            Assert.IsTrue(select.Success);
            Assert.AreEqual(-1, select.RecordsAffected);
            Assert.IsNull(select.Error);
            Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Select single
            var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
            Assert.IsTrue(selectSingle.Success);
            Assert.AreEqual(-1, selectSingle.RecordsAffected);
            Assert.IsNull(selectSingle.Error);
            Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Update
            var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
            Assert.IsTrue(update.Success);
            Assert.AreEqual(1, update.RecordsAffected);
            Assert.IsNull(update.Error);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));
            var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
            Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
            Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
            Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Delete
            var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
            Assert.IsTrue(delete.Success);
            Assert.AreEqual(1, delete.RecordsAffected);
            Assert.IsNull(delete.Error);
            Assert.AreEqual(2, Helper.GetRowCount(TableName));
            var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelectAfterExecution, options, default);
            Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);

            CleanUp();
            Init();
        }
    }

    [TestMethod]
    public async Task TestExecuteProcedure_ProcedureParameter()
    {
        var parameter = new ProcedureParameter
        {
            Name = "lastname",
            Value = "Parametri",
            SqlDataType = SqlDataTypes.Auto,
        };

        var parameterInput = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "InsertWithParameter",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = new ProcedureParameter[] { parameter },
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true,
        };

        var insert = await MicrosoftSQL.ExecuteProcedure(parameterInput, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.Error);
    }

    [TestMethod]
    public async Task TestExecuteProcedure_ProcedureNullParameter()
    {
        var parameter = new ProcedureParameter
        {
            Name = "lastname",
            Value = null,
            SqlDataType = SqlDataTypes.Auto,
        };

        var parameterInput = new Input()
        {
            ConnectionString = ConnectionString,
            Execute = "InsertWithParameter",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = new ProcedureParameter[] { parameter },
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true,
        };

        var insert = await MicrosoftSQL.ExecuteProcedure(parameterInput, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(1, insert.RecordsAffected);
        Assert.IsNull(insert.Error);
    }
}
