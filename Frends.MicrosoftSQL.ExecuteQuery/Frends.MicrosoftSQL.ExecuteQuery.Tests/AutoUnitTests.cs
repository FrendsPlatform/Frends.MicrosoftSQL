using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class AutoUnitTests : ExecuteQueryTestBase
{
    [TestMethod]
    public async Task TestExecuteQuery_Auto()
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
            Query = $@"INSERT INTO {TableName} VALUES (1, 'Suku', 'Etu'), (2, 'Last', 'Forst'), (3, 'Hiiri', 'Mikki')",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputSelect = new Input()
        {
            Query = $"select * from {TableName}",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null,
        };

        var inputSelectSingle = new Input()
        {
            Query = $"select * from {TableName} where Id = 1",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null,
        };

        var inputUpdate = new Input()
        {
            Query = $@"update {TableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null,
        };

        var inputDelete = new Input()
        {
            Query = $"delete from {TableName} where Id = 2",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null,
        };

        foreach (var level in transactionLevels)
        {
            Init();

            var options = new Options()
            {
                SqlTransactionIsolationLevel = level,
                CommandTimeoutSeconds = 2,
                ThrowErrorOnFailure = true,
            };

            // Insert rows
            var insert = await MicrosoftSQL.ExecuteQuery(inputInsert, CreateConnection(), options, default);
            Assert.IsTrue(insert.Success);
            Assert.AreEqual(3, insert.RecordsAffected);
            Assert.IsNull(insert.Error);
            Assert.AreEqual(3, (int)insert.Data["AffectedRows"]);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // Make sure rows inserted before moving on.

            // Select all
            var select = await MicrosoftSQL.ExecuteQuery(inputSelect, CreateConnection(), options, default);
            Assert.IsTrue(select.Success);
            Assert.AreEqual(-1, select.RecordsAffected);
            Assert.IsNull(select.Error);
            Assert.AreEqual(typeof(JArray), select.Data.GetType());
            Assert.AreEqual("Suku", (string)select.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)select.Data[0]["FirstName"]);
            Assert.AreEqual("Last", (string)select.Data[1]["LastName"]);
            Assert.AreEqual("Forst", (string)select.Data[1]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)select.Data[2]["LastName"]);
            Assert.AreEqual("Mikki", (string)select.Data[2]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check

            // Select single
            var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, CreateConnection(), options, default);
            Assert.IsTrue(selectSingle.Success);
            Assert.AreEqual(-1, selectSingle.RecordsAffected);
            Assert.IsNull(selectSingle.Error);
            Assert.AreEqual(typeof(JArray), selectSingle.Data.GetType());
            Assert.AreEqual("Suku", (string)selectSingle.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)selectSingle.Data[0]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check

            // Update
            var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, CreateConnection(), options, default);
            Assert.IsTrue(update.Success);
            Assert.AreEqual(1, update.RecordsAffected);
            Assert.IsNull(update.Error);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check
            var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, CreateConnection(), options, default);
            Assert.AreEqual("Suku", (string)checkUpdateResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkUpdateResult.Data[0]["FirstName"]);
            Assert.AreEqual("Edit", (string)checkUpdateResult.Data[1]["LastName"]);
            Assert.AreEqual("Forst", (string)checkUpdateResult.Data[1]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkUpdateResult.Data[2]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkUpdateResult.Data[2]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check

            // Delete
            var delete = await MicrosoftSQL.ExecuteQuery(inputDelete, CreateConnection(), options, default);
            Assert.IsTrue(delete.Success);
            Assert.AreEqual(1, delete.RecordsAffected);
            Assert.IsNull(delete.Error);
            Assert.AreEqual(2, Helper.GetRowCount(ConnString, TableName)); // double check
            var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, CreateConnection(), options, default);
            Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestWithGeographyData()
    {
        var table = "geographytest";

        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            CommandTimeoutSeconds = 2,
            ThrowErrorOnFailure = true,
        };

        var input = new Input
        {
            Query = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}') BEGIN CREATE TABLE {table} ( Id int IDENTITY(1, 1), GeogCol1 geography, GeogCol2 AS GeogCol1.STAsText()); END",
            ExecuteType = ExecuteTypes.Auto,
            Parameters = null,
        };

        try
        {
            var create = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
            Assert.IsTrue(create.Success, "Create table");

            input.Query = $"INSERT INTO {table} (GeogCol1) VALUES (geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656 )', 4326));";

            var insert1 = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
            Assert.IsTrue(insert1.Success, "First insert");

            input.Query = $"INSERT INTO {table} (GeogCol1) VALUES(geography::STGeomFromText('POLYGON((-122.358 47.653 , -122.348 47.649, -122.348 47.658, -122.358 47.658, -122.358 47.653))', 4326));";

            var insert2 = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
            Assert.IsTrue(insert2.Success, "Second insert");

            input.Query = $"SELECT * From {table}";

            var select = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
            Assert.IsTrue(select.Success, "Select");
            Assert.AreEqual(typeof(JArray), select.Data.GetType());
            Assert.AreEqual(2, select.Data.Count);

            // Verify first row (LINESTRING)
            Assert.IsNotNull(select.Data[0]["GeogCol1"]);
            Assert.IsTrue(select.Data[0]["GeogCol2"].ToString().StartsWith("LINESTRING"));

            // Verify second row (POLYGON)
            Assert.IsNotNull(select.Data[1]["GeogCol1"]);
            Assert.IsTrue(select.Data[1]["GeogCol2"].ToString().StartsWith("POLYGON"));
        }
        finally
        {
            input.Query = $"DROP TABLE {table}";

            var drop = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
            Assert.IsTrue(drop.Success, "Drop");
        }
    }
}
