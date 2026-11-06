using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class NonQueryUnitTests : ExecuteQueryTestBase
{
    [TestMethod]
    public async Task TestExecuteQuery_NonQuery()
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
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputSelectSingle = new Input()
        {
            Query = $"select * from {TableName} where Id = 1",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputUpdate = new Input()
        {
            Query = $@"update {TableName} set LastName = 'Edit' where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputDelete = new Input()
        {
            Query = $"delete from {TableName} where Id = 2",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        var inputSelectAfterExecution = new Input()
        {
            Query = $"select * from {TableName}",
            ExecuteType = ExecuteTypes.ExecuteReader,
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
            Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check

            // Select single
            var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, CreateConnection(), options, default);
            Assert.IsTrue(selectSingle.Success);
            Assert.AreEqual(-1, selectSingle.RecordsAffected);
            Assert.IsNull(selectSingle.Error);
            Assert.AreEqual(-1, (int)select.Data["AffectedRows"]);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check

            // Update
            var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, CreateConnection(), options, default);
            Assert.IsTrue(update.Success);
            Assert.AreEqual(1, update.RecordsAffected);
            Assert.IsNull(update.Error);
            Assert.AreEqual(3, Helper.GetRowCount(ConnString, TableName)); // double check
            var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, CreateConnection(), options, default);
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
            var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelectAfterExecution, CreateConnection(), options, default);
            Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);

            CleanUp();
        }
    }

    [TestMethod]
    public async Task TestExecuteQuery_DBNullValues()
    {
        var options = new Options()
        {
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.Default,
            CommandTimeoutSeconds = 30,
            ThrowErrorOnFailure = true,
        };

        var input = new Input()
        {
            Query = $@"INSERT INTO {TableName} VALUES (1, @Last, @First)",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = new QueryParameter[]
            {
                new()
                {
                    Name = "@Last",
                    Value = null,
                    SqlDataType = SqlDataTypes.Auto,
                },
                new()
                {
                    Name = "@First",
                    Value = "Mikki",
                    SqlDataType = SqlDataTypes.Auto,
                },
            },
        };

        var result = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
        Assert.IsTrue(result.Success);

        input.Query = $"SELECT * FROM {TableName}";
        input.ExecuteType = ExecuteTypes.Auto;
        var selectResult = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
        Assert.AreEqual(null, (string)selectResult.Data[0]["LastName"]);
        Assert.AreEqual("Mikki", (string)selectResult.Data[0]["FirstName"]);

        var jToken = new JObject(
            new JProperty("Etu", "Mikki"));

        input.Parameters = new QueryParameter[]
            {
                new()
                {
                    Name = "@Last",
                    Value = jToken["Suku"],
                    SqlDataType = SqlDataTypes.Auto,
                },
                new()
                {
                    Name = "@First",
                    Value = jToken["Etu"],
                    SqlDataType = SqlDataTypes.Auto,
                },
            };

        result = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
        Assert.IsTrue(result.Success);

        input.Query = $"SELECT * FROM {TableName}";
        input.ExecuteType = ExecuteTypes.Auto;
        selectResult = await MicrosoftSQL.ExecuteQuery(input, CreateConnection(), options, default);
        Assert.AreEqual(null, (string)selectResult.Data[0]["LastName"]);
        Assert.AreEqual("Mikki", (string)selectResult.Data[0]["FirstName"]);
    }
}
