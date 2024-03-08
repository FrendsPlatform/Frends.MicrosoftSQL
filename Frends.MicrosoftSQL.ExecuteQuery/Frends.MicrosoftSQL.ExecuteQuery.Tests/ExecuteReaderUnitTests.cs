using Frends.MicrosoftSQL.ExecuteQuery.Definitions;
using Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests;

[TestClass]
public class ExecuteReaderUnitTests : ExecuteQueryTestBase
{
    [TestMethod]
    public async Task TestExecuteQuery_ExecuteReader()
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
            Assert.AreEqual(3, insert.RecordsAffected);
            Assert.IsNull(insert.ErrorMessage);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // Make sure rows inserted before moving on.

            // Select all
            var select = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
            Assert.IsTrue(select.Success);
            Assert.AreEqual(-1, select.RecordsAffected);
            Assert.IsNull(select.ErrorMessage);
            Assert.AreEqual(typeof(JArray), select.Data.GetType());
            Assert.AreEqual("Suku", (string)select.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)select.Data[0]["FirstName"]);
            Assert.AreEqual("Last", (string)select.Data[1]["LastName"]);
            Assert.AreEqual("Forst", (string)select.Data[1]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)select.Data[2]["LastName"]);
            Assert.AreEqual("Mikki", (string)select.Data[2]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check

            // Select single
            var selectSingle = await MicrosoftSQL.ExecuteQuery(inputSelectSingle, options, default);
            Assert.IsTrue(selectSingle.Success);
            Assert.AreEqual(-1, selectSingle.RecordsAffected);
            Assert.IsNull(selectSingle.ErrorMessage);
            Assert.AreEqual(typeof(JArray), selectSingle.Data.GetType());
            Assert.AreEqual("Suku", (string)selectSingle.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)selectSingle.Data[0]["FirstName"]);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check

            // Update
            var update = await MicrosoftSQL.ExecuteQuery(inputUpdate, options, default);
            Assert.IsTrue(update.Success);
            Assert.AreEqual(1, update.RecordsAffected);
            Assert.IsNull(update.ErrorMessage);
            Assert.AreEqual(3, Helper.GetRowCount(_connString, _tableName)); // double check
            var checkUpdateResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
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
            var checkDeleteResult = await MicrosoftSQL.ExecuteQuery(inputSelect, options, default);
            Assert.AreEqual("Suku", (string)checkDeleteResult.Data[0]["LastName"]);
            Assert.AreEqual("Etu", (string)checkDeleteResult.Data[0]["FirstName"]);
            Assert.AreEqual("Hiiri", (string)checkDeleteResult.Data[1]["LastName"]);
            Assert.AreEqual("Mikki", (string)checkDeleteResult.Data[1]["FirstName"]);

            CleanUp();
        }
    }

    [TestMethod]
    public async Task ExecuteQueryTestWithBinaryData()
    {
        var table = "binarytest";
        var command = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}') BEGIN CREATE TABLE {table} ( Id int, Data varbinary(MAX)); END";
        ExecuteQuery(command);

        var binary = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestData/", "Test_image.png"));

        var input = new Input
        {
            ConnectionString = _connString,
            Query = $"INSERT INTO {table} VALUES (1, @bin)",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = new QueryParameter[] { new QueryParameter { Name = "bin", Value = binary, SqlDataType = SqlDataTypes.VarBinary } }
        };

        var options = new Options
        {
            CommandTimeoutSeconds = 30,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            ThrowErrorOnFailure = true
        };

        var result = await MicrosoftSQL.ExecuteQuery(input, options, default);
        Assert.IsTrue(result.Success);

        input = new Input
        {
            ConnectionString = _connString,
            Query = $@"SELECT Id, Data From {table}",
            ExecuteType = ExecuteTypes.ExecuteReader,
            Parameters = null
        };

        result = await MicrosoftSQL.ExecuteQuery(input, options, default);

        command = $"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}') BEGIN DROP TABLE IF EXISTS {table}; END";
        ExecuteQuery(command);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(Convert.ToBase64String(binary), Convert.ToBase64String((byte[])result.Data[0]["Data"]));
    }

    private static void ExecuteQuery(string command)
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = command;
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }
}