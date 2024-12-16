using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
public class ExecuteReaderUnitTests
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

        //Select single parameter
        command.CommandText = $"CREATE PROCEDURE SelectSingleParameter (@id INT) AS SELECT * FROM {_tableName} WHERE Id = @id";
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
    public async Task TestExecuteProcedure_ExecuteReader()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            //SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelect = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputSelectSingle = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputUpdate = new Input()
        {
            ConnectionString = _connString,
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var inputDelete = new Input()
        {
            ConnectionString = _connString,
            Execute = "DeleteValue",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        foreach (var level in transactionLevels)
        {
            var options = new Options()
            {
                SqlTransactionIsolationLevel = level,
                CommandTimeoutSeconds = 2,
                ThrowErrorOnFailure = true
            };

            // Insert rows
            var insert = await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);
            Assert.IsTrue(insert.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(3, insert.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(insert.ErrorMessage, $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(_tableName), $"TransactionLevel: {level}.");

            // Select all
            var select = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
            Assert.IsTrue(select.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(-1, select.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(select.ErrorMessage, $"TransactionLevel: {level}.");
            Assert.AreEqual(typeof(JArray), select.Data.GetType(), $"TransactionLevel: {level}.");
            Assert.AreEqual("Suku", (string)select.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)select.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Last", (string)select.Data[1]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Forst", (string)select.Data[1]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Hiiri", (string)select.Data[2]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Mikki", (string)select.Data[2]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(_tableName), $"TransactionLevel: {level}.");

            // Select single
            var selectSingle = await MicrosoftSQL.ExecuteProcedure(inputSelectSingle, options, default);
            Assert.IsTrue(selectSingle.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(-1, selectSingle.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(selectSingle.ErrorMessage, $"TransactionLevel: {level}.");
            Assert.AreEqual(typeof(JArray), selectSingle.Data.GetType(), $"TransactionLevel: {level}.");
            Assert.AreEqual("Suku", (string)selectSingle.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)selectSingle.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(_tableName));

            // Update
            var update = await MicrosoftSQL.ExecuteProcedure(inputUpdate, options, default);
            Assert.IsTrue(update.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(1, update.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(update.ErrorMessage, $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(_tableName), $"TransactionLevel: {level}.");
            var checkUpdateResult = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
            Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(_tableName));

            // Delete
            var delete = await MicrosoftSQL.ExecuteProcedure(inputDelete, options, default);
            Assert.IsTrue(delete.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(1, delete.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(delete.ErrorMessage, $"TransactionLevel: {level}.");
            Assert.AreEqual(2, Helper.GetRowCount(_tableName), $"TransactionLevel: {level}.");
            var checkDeleteResult = await MicrosoftSQL.ExecuteProcedure(inputSelect, options, default);
            Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"], $"TransactionLevel: {level}.");

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
            SqlDataType = SqlDataTypes.Auto
        };

        var inputInsert = new Input()
        {
            ConnectionString = _connString,
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        var parameterInput = new Input()
        {
            ConnectionString = _connString,
            Execute = "SelectSingleParameter",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = new ProcedureParameter[] { parameter }
        };

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        await MicrosoftSQL.ExecuteProcedure(inputInsert, options, default);

        var query = await MicrosoftSQL.ExecuteProcedure(parameterInput, options, default);
        Assert.IsTrue(query.Success);
        Assert.AreEqual(-1, query.RecordsAffected);
        Assert.IsNull(query.ErrorMessage);
        Assert.AreEqual(1, (int)query.Data[0]["Id"]);
        Assert.AreEqual("Suku", (string)query.Data[0]["LastName"]);
    }

    [TestMethod]
    public async Task ExecuteQueryTestWithBinaryData()
    {
        var table = "binarytest";
        var command = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}') BEGIN CREATE TABLE {table} ( Id int, Data varbinary(MAX)); END";
        Helper.ExecuteNonQuery(command);

        var binary = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestData/", "Test_image.png"));

        //Insert
        command = $"CREATE PROCEDURE InsertBinaryValues(@binary as varbinary(MAX)) AS INSERT INTO {table} (Id, Data) VALUES (1, @binary)";
        Helper.ExecuteNonQuery(command);

        var input = new Input
        {
            ConnectionString = _connString,
            Execute = "InsertBinaryValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = new ProcedureParameter[] { new ProcedureParameter { Name = "binary", Value = binary, SqlDataType = SqlDataTypes.VarBinary } }
        };

        var options = new Options
        {
            CommandTimeoutSeconds = 30,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            ThrowErrorOnFailure = true
        };

        var result = await MicrosoftSQL.ExecuteProcedure(input, options, default);
        Assert.IsTrue(result.Success);

        //Select single
        command = $"CREATE PROCEDURE SelectSingleBinary AS select * from {table} where Id = 1";
        Helper.ExecuteNonQuery(command);

        input = new Input
        {
            ConnectionString = _connString,
            Execute = "SelectSingleBinary",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        result = await MicrosoftSQL.ExecuteProcedure(input, options, default);

        command = $"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}') BEGIN DROP TABLE IF EXISTS {table}; END";
        Helper.ExecuteNonQuery(command);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(Convert.ToBase64String(binary), Convert.ToBase64String((byte[])result.Data[0]["Data"]));
    }

    [TestMethod]
    public async Task TestWithGeographyData()
    {
        var table = "geographytest";

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 30,
            ThrowErrorOnFailure = true
        };

        var input = new Input
        {
            ConnectionString = _connString,
            Execute = "",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null
        };

        // Create new table
        var command = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}') BEGIN CREATE TABLE {table} ( Id int IDENTITY(1, 1), GeogCol1 geography, GeogCol2 AS GeogCol1.STAsText()); END";
        Helper.ExecuteNonQuery(command);

        command = $"CREATE PROCEDURE InsertGeography AS INSERT INTO {table} (GeogCol1) VALUES (geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656 )', 4326));";
        Helper.ExecuteNonQuery(command);

        command = $"CREATE PROCEDURE InsertGeometry AS INSERT INTO {table} (GeogCol1) VALUES(geography::STGeomFromText('POLYGON((-122.358 47.653 , -122.348 47.649, -122.348 47.658, -122.358 47.658, -122.358 47.653))', 4326));";
        Helper.ExecuteNonQuery(command);

        command = $"CREATE PROCEDURE SelectGeography AS select * from {table}";
        Helper.ExecuteNonQuery(command);
        
        try
        {
            
            input.Execute = "InsertGeography";

            var result = await MicrosoftSQL.ExecuteProcedure(input, options, default);
            Assert.IsTrue(result.Success, "First insert");

            input.Execute = "InsertGeometry";

            result = await MicrosoftSQL.ExecuteProcedure(input, options, default);
            Assert.IsTrue(result.Success, "Second insert");

            input.Execute = "SelectGeography";
            input.ExecuteType = ExecuteTypes.ExecuteReader;

            result = await MicrosoftSQL.ExecuteProcedure(input, options, default);
            Assert.IsTrue(result.Success, "Select");
            Assert.AreEqual(typeof(JArray), result.Data.GetType());
            Assert.AreEqual(2, result.Data.Count);

            // Verify first row (LINESTRING)
            Assert.IsNotNull(result.Data[0]["GeogCol1"]);
            Assert.IsTrue(result.Data[0]["GeogCol2"].ToString().StartsWith("LINESTRING"));

            // Verify second row (POLYGON)
            Assert.IsNotNull(result.Data[1]["GeogCol1"]);
            Assert.IsTrue(result.Data[1]["GeogCol2"].ToString().StartsWith("POLYGON"));
        }
        finally
        {
            command = $"DROP TABLE {table}";
            Helper.ExecuteNonQuery(command);
        }
    }
}