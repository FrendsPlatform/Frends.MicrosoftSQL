using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.SqlClient;

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
public class ManualTesting
{
    private static readonly string _connString = Helper.GetConnectionString();
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

    // Use 'throw new Exception();' in HandleExecutionException class.
    [Ignore("Use 'throw new Exception();' in HandleExecutionException class.")]
    [TestMethod]
    public async Task TestExecuteProcedure_RollbackInsert_ThrowErrorOnFailure_False()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
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
        var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
        Assert.IsFalse(insert.Success);
        Assert.AreEqual(0, insert.RecordsAffected);
        Assert.IsTrue(insert.ErrorMessage.Contains("ExecuteHandler exception: (If required) transaction rollback completed without exception."));
        Assert.AreEqual(0, Helper.GetRowCount(_tableName));
    }

    // Use 'throw new Exception();' in HandleExecutionException class.
    [Ignore("Use 'throw new Exception();' in HandleExecutionException class.")]
    [TestMethod]
    public async Task TestExecuteProcedure_RollbackInsert_ThrowErrorOnFailure_True()
    {
        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
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
        var insert = await Assert.ThrowsExceptionAsync<Exception>(async () => await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default));
        Assert.IsTrue(insert.Message.Contains("ExecuteHandler exception: (If required) transaction rollback completed without exception."));
    }
}