using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ScalarUnitTests : ExecuteQueryTestBase
{
    [TestMethod]
    public async Task TestExecuteQuery_Scalar()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>() {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted
        };

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

        foreach (var level in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = level,
                CommandTimeoutSeconds = 2,
                ThrowErrorOnFailure = true
            };

            // Insert rows
            var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, options, default);
            Assert.IsTrue(insert.Success);
            Assert.AreEqual(1, insert.RecordsAffected);
            Assert.IsNull(insert.ErrorMessage);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // Make sure rows inserted before moving on.

            // Select all - Returns 1 because first value (row/column) is ID = 1
            var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
            Assert.IsTrue(select.Success);
            Assert.AreEqual(1, select.RecordsAffected);
            Assert.IsNull(select.ErrorMessage);
            Assert.AreEqual(1, (int)select.Data["Value"]);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check

            // Select single
            var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
            Assert.IsTrue(selectSingle.Success);
            Assert.AreEqual(1, selectSingle.RecordsAffected);
            Assert.IsNull(selectSingle.ErrorMessage);
            Assert.AreEqual("Suku", (string)selectSingle.Data["Value"]);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check

            // Update
            var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
            Assert.IsTrue(update.Success);
            Assert.AreEqual(1, update.RecordsAffected);
            Assert.IsNull(update.ErrorMessage);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check
            var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
            Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
            Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
            Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check

            // Delete
            var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, options, default);
            Assert.IsTrue(delete.Success);
            Assert.AreEqual(1, delete.RecordsAffected);
            Assert.IsNull(delete.ErrorMessage);
            Assert.AreEqual(2, Helper.GetRowCount(_connString, _tableName)); // double check
            var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, options, default);
            Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestWithGeographyData_Scalar()
    {
        var table = "geographytest";

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true
        };

        var input = new Input
        {
            ConnectionString = _connString,
            Query = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}') BEGIN CREATE TABLE {table} ( Id int IDENTITY(1, 1), GeogCol1 geography, GeogCol2 AS GeogCol1.STAsText()); END",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null
        };

        try
        {
            var create = await MicrosoftSQL.ExecuteQuery(input, options, default);
            Assert.IsTrue(create.Success, "Create table");

            input.Query = $"INSERT INTO {table} (GeogCol1) VALUES (geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656 )', 4326));";

            var insert1 = await MicrosoftSQL.ExecuteQuery(input, options, default);
            Assert.IsTrue(insert1.Success, "First insert");

            input.Query = $"INSERT INTO {table} (GeogCol1) VALUES(geography::STGeomFromText('POLYGON((-122.358 47.653 , -122.348 47.649, -122.348 47.658, -122.358 47.658, -122.358 47.653))', 4326));";

            var insert2 = await MicrosoftSQL.ExecuteQuery(input, options, default);
            Assert.IsTrue(insert2.Success, "Second insert");

            input.Query = $"SELECT GeogCol1 From {table}";
            input.ExecuteType = ExecuteTypes.Scalar;

            var select = await MicrosoftSQL.ExecuteQuery(input, options, default);
            Assert.IsTrue(select.Success, "Select");
            Assert.IsNotNull(select.Data["Value"], "Selected data should not be null");
            Assert.IsInstanceOfType(select.Data["Value"], typeof(JValue), "Geography data should be converted to string");
            Assert.IsTrue(((string)select.Data["Value"]).StartsWith("LINESTRING"), "First row should be a LINESTRING");
        }
        finally
        {
            input.Query = $"DROP TABLE {table}";
            input.ExecuteType = ExecuteTypes.Auto;

            var drop = await MicrosoftSQL.ExecuteQuery(input, options, default);
            Assert.IsTrue(drop.Success, "Drop");
        }
    }
}