using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class ScalarUnitTests
{
    private static readonly string TableName = "TestTable";

    private static string ConnectionString => Helper.GetConnectionString();

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
        command.CommandText = $"CREATE PROCEDURE SelectSingleParameter (@id INT) AS SELECT * FROM {TableName} WHERE Id = @id";
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
    public async Task TestExecuteProcedure_Scalar()
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
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null,
        };

        var inputSelect = new Input()
        {
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null,
        };

        var inputSelectSingle = new Input()
        {
            Execute = "SelectLastname",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null,
        };

        var inputUpdate = new Input()
        {
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null,
        };

        var inputDelete = new Input()
        {
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null,
        };

        var inputSelectAfterExecution = new Input()
        {
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        foreach (var options in transactionLevels.Select(e => new Options { SqlTransactionIsolationLevel = e, CommandTimeoutSeconds = 2, ThrowErrorOnFailure = true }))
        {
            // Insert rows
            var insert = await Helper.ExecuteProcedure(inputInsert, options, default);
            Assert.IsTrue(insert.Success);
            Assert.AreEqual(1, insert.RecordsAffected);
            Assert.IsNull(insert.Error);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Select all - Returns 1 because first value (row/column) is ID = 1
            var select = await Helper.ExecuteProcedure(inputSelect, options, default);
            Assert.IsTrue(select.Success);
            Assert.AreEqual(1, select.RecordsAffected);
            Assert.IsNull(select.Error);
            Assert.AreEqual(1, (int)select.Data["Value"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Select single
            var selectSingle = await Helper.ExecuteProcedure(inputSelectSingle, options, default);
            Assert.IsTrue(selectSingle.Success);
            Assert.AreEqual(1, selectSingle.RecordsAffected);
            Assert.IsNull(selectSingle.Error);
            Assert.AreEqual("Suku", (string)selectSingle.Data["Value"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Update
            var update = await Helper.ExecuteProcedure(inputUpdate, options, default);
            Assert.IsTrue(update.Success);
            Assert.AreEqual(1, update.RecordsAffected);
            Assert.IsNull(update.Error);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));
            var checkUpdateResult = await Helper.ExecuteProcedure(inputSelectAfterExecution, options, default);
            Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
            Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
            Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Delete
            var delete = await Helper.ExecuteProcedure(inputDelete, options, default);
            Assert.IsTrue(delete.Success);
            Assert.AreEqual(1, delete.RecordsAffected);
            Assert.IsNull(delete.Error);
            Assert.AreEqual(2, Helper.GetRowCount(TableName));
            var checkDeleteResult = await Helper.ExecuteProcedure(inputSelectAfterExecution, options, default);
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
            Name = "id",
            Value = "1",
            SqlDataType = SqlDataTypes.Auto,
        };

        var inputInsert = new Input()
        {
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.Scalar,
            Parameters = null,
        };

        var parameterInput = new Input()
        {
            Execute = "SelectSingleParameter",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = new ProcedureParameter[] { parameter },
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true,
        };

        await Helper.ExecuteProcedure(inputInsert, options, default);

        var insert = await Helper.ExecuteProcedure(parameterInput, options, default);
        Assert.IsTrue(insert.Success);
        Assert.AreEqual(-1, insert.RecordsAffected);
        Assert.IsNull(insert.Error);
        Assert.AreEqual(1, (int)insert.Data[0]["Id"]);
        Assert.AreEqual("Suku", (string)insert.Data[0]["LastName"]);
    }
}
