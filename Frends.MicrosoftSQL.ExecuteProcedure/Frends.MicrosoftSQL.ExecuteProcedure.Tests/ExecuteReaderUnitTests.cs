using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class ExecuteReaderUnitTests
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

        // Select special values
        command.CommandText = "CREATE PROCEDURE SelectNull AS SELECT CAST(NULL AS INT) AS NullValue";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE PROCEDURE SelectGeographyValue AS SELECT geography::Point(47.651, -122.349, 4326) AS Value";
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
    public async Task TestExecuteProcedure_ExecuteReader()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            // SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
            SqlTransactionIsolationLevel.RepeatableRead,
            (SqlTransactionIsolationLevel)int.MaxValue,
        };

        var inputInsert = new Input()
        {
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var inputSelect = new Input()
        {
            Execute = "SelectAll",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var inputSelectSingle = new Input()
        {
            Execute = "SelectSingle",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var inputUpdate = new Input()
        {
            Execute = "UpdateValue",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        var inputDelete = new Input()
        {
            Execute = "DeleteValue",
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
            var insert = await Helper.ExecuteProcedure(inputInsert, options, default);
            Assert.IsTrue(insert.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(3, insert.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(insert.Error, $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(TableName), $"TransactionLevel: {level}.");

            // Select all
            var select = await Helper.ExecuteProcedure(inputSelect, options, default);
            Assert.IsTrue(select.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(-1, select.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(select.Error, $"TransactionLevel: {level}.");
            Assert.AreEqual(typeof(JArray), select.Data.GetType(), $"TransactionLevel: {level}.");
            Assert.AreEqual("Suku", (string)select.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)select.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Last", (string)select.Data[1]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Forst", (string)select.Data[1]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Hiiri", (string)select.Data[2]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Mikki", (string)select.Data[2]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(TableName), $"TransactionLevel: {level}.");

            // Select single
            var selectSingle = await Helper.ExecuteProcedure(inputSelectSingle, options, default);
            Assert.IsTrue(selectSingle.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(-1, selectSingle.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(selectSingle.Error, $"TransactionLevel: {level}.");
            Assert.AreEqual(typeof(JArray), selectSingle.Data.GetType(), $"TransactionLevel: {level}.");
            Assert.AreEqual("Suku", (string)selectSingle.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)selectSingle.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Update
            var update = await Helper.ExecuteProcedure(inputUpdate, options, default);
            Assert.IsTrue(update.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(1, update.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(update.Error, $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(TableName), $"TransactionLevel: {level}.");
            var checkUpdateResult = await Helper.ExecuteProcedure(inputSelect, options, default);
            Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"], $"TransactionLevel: {level}.");
            Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"], $"TransactionLevel: {level}.");
            Assert.AreEqual(3, Helper.GetRowCount(TableName));

            // Delete
            var delete = await Helper.ExecuteProcedure(inputDelete, options, default);
            Assert.IsTrue(delete.Success, $"TransactionLevel: {level}.");
            Assert.AreEqual(1, delete.RecordsAffected, $"TransactionLevel: {level}.");
            Assert.IsNull(delete.Error, $"TransactionLevel: {level}.");
            Assert.AreEqual(2, Helper.GetRowCount(TableName), $"TransactionLevel: {level}.");
            var checkDeleteResult = await Helper.ExecuteProcedure(inputSelect, options, default);
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
            SqlDataType = SqlDataTypes.Auto,
        };

        var inputInsert = new Input()
        {
            Execute = "InsertValues",
            ExecuteType = ExecuteTypes.ExecuteReader,
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

        var query = await Helper.ExecuteProcedure(parameterInput, options, default);
        Assert.IsTrue(query.Success);
        Assert.AreEqual(-1, query.RecordsAffected);
        Assert.IsNull(query.Error);
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

        // Insert
        command = $"CREATE PROCEDURE InsertBinaryValues(@binary as varbinary(MAX)) AS INSERT INTO {table} (Id, Data) VALUES (1, @binary)";
        Helper.ExecuteNonQuery(command);

        var input = new Input
        {
            Execute = "InsertBinaryValues",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = new ProcedureParameter[] { new ProcedureParameter { Name = "binary", Value = binary, SqlDataType = SqlDataTypes.VarBinary } },
        };

        var options = new Options
        {
            CommandTimeoutSeconds = 30,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            ThrowErrorOnFailure = true,
        };

        var result = await Helper.ExecuteProcedure(input, options, default);
        Assert.IsTrue(result.Success);

        // Select single
        command = $"CREATE PROCEDURE SelectSingleBinary AS select * from {table} where Id = 1";
        Helper.ExecuteNonQuery(command);

        input = new Input
        {
            Execute = "SelectSingleBinary",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null,
        };

        result = await Helper.ExecuteProcedure(input, options, default);

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
            ThrowErrorOnFailure = true,
        };

        var input = new Input
        {
            Execute = string.Empty,
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
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

            var result = await Helper.ExecuteProcedure(input, options, default);
            Assert.IsTrue(result.Success, "First insert");

            input.Execute = "InsertGeometry";

            result = await Helper.ExecuteProcedure(input, options, default);
            Assert.IsTrue(result.Success, "Second insert");

            input.Execute = "SelectGeography";
            input.ExecuteType = ExecuteTypes.ExecuteReader;

            result = await Helper.ExecuteProcedure(input, options, default);
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
            Helper.ExecuteNonQuery("IF OBJECT_ID('InsertGeography') IS NOT NULL DROP PROCEDURE InsertGeography");
            Helper.ExecuteNonQuery("IF OBJECT_ID('InsertGeometry') IS NOT NULL DROP PROCEDURE InsertGeometry");
            Helper.ExecuteNonQuery("IF OBJECT_ID('SelectGeography') IS NOT NULL DROP PROCEDURE SelectGeography");

            Helper.ExecuteNonQuery($"DROP TABLE {table}");
        }
    }

    [TestMethod]
    public async Task ExecuteScalar_ConvertsGeographyValueToText()
    {
        var result = await Helper.ExecuteProcedure(
            new Input
            {
                Execute = "SelectGeographyValue",
                ExecuteType = ExecuteTypes.Scalar,
            },
            new Options { SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None },
            default);

        Assert.IsTrue(result.Success, result.Error?.Message);
        Assert.IsTrue(result.Data["Value"].ToString().StartsWith("POINT"));
    }

    [TestMethod]
    public async Task ExecuteReader_MapsDatabaseNullToJsonNull()
    {
        var result = await Helper.ExecuteProcedure(
            new Input
            {
                Execute = "SelectNull",
                ExecuteType = ExecuteTypes.ExecuteReader,
            },
            new Options { SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None },
            default);

        Assert.IsTrue(result.Success, result.Error?.Message);
        Assert.AreEqual(JTokenType.Null, result.Data[0]["NullValue"].Type);
    }

    [TestMethod]
    public async Task UnsupportedExecuteType_ReturnsError()
    {
        var result = await Helper.ExecuteProcedure(
            new Input
            {
                Execute = "SelectAll",
                ExecuteType = (ExecuteTypes)int.MaxValue,
            },
            new Options
            {
                SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
                ThrowErrorOnFailure = false,
            },
            default);

        Assert.IsFalse(result.Success);
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(NotSupportedException));
    }
}
