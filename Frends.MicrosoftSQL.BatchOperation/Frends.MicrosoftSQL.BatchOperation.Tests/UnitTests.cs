using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Frends.MicrosoftSQL.BatchOperation.Tests.Lib;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

[TestClass]
public class UnitTests
{
    private static readonly string TableName = "TestTable";

    private static string ConnString => SqlServerContainer.ConnectionString;

    [TestInitialize]
    public void Init()
    {
        using var connection = new SqlConnection(ConnString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{TableName}') BEGIN CREATE TABLE {TableName} ( Id int, LastName varchar(255), FirstName varchar(255) ); END";
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }

    [TestCleanup]
    public void CleanUp()
    {
        using var connection = new SqlConnection(ConnString);
        connection.Open();
        var createTable = connection.CreateCommand();
        createTable.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{TableName}') BEGIN DROP TABLE IF EXISTS {TableName}; END";
        createTable.ExecuteNonQuery();
        connection.Close();
        connection.Dispose();
    }

    [TestMethod]
    public async Task TestBatchOperation()
    {
        var transactionLevels = new List<SqlTransactionIsolationLevel>()
        {
            SqlTransactionIsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.Serializable,
            SqlTransactionIsolationLevel.None,
            SqlTransactionIsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted,
            SqlTransactionIsolationLevel.RepeatableRead,
            (SqlTransactionIsolationLevel)int.MaxValue,
        };

        var inputInsert = new Input()
        {
            Query = $@"INSERT INTO {TableName} VALUES (@Id, @last_name, @first_name)",
            InputJson = "[{\"Id\":1,\"last_name\":\"Suku\",\"first_name\":\"Etu\"},{\"Id\":2,\"last_name\":\"Last\",\"first_name\":\"Forst\"},{\"Id\":3,\"last_name\":\"Hiiri\",\"first_name\":\"Mikki\"}]",
        };

        var inputUpdateSingle = new Input()
        {
            Query = $@"UPDATE {TableName} SET LastName = @last_name WHERE Id = @Id",
            InputJson = "[{\"Id\":2,\"last_name\":\"Edit\"}]",
        };

        var inputUpdateMultiple = new Input()
        {
            Query = $@"UPDATE {TableName} SET LastName = @last_name WHERE Id = @Id",
            InputJson = "[{\"Id\":1,\"last_name\":\"Foobar\"},{\"Id\":3,\"last_name\":\"Foobar\"}]",
        };

        var inputDeleteSingle = new Input()
        {
            Query = $"delete from {TableName} where Id = 2",
            InputJson = "[{\"Id\":2}]",
        };

        var inputDeleteMultiple = new Input()
        {
            Query = $"delete from {TableName} where Id = @Id",
            InputJson = "[{\"Id\":1}, {\"Id\":3}]",
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
            var insert = await TaskRunner.BatchOperation(inputInsert, options, default);
            Assert.IsTrue(insert.Success);
            Assert.AreEqual(3, insert.RecordsAffected);
            Assert.IsNull(insert.Error);
            Assert.AreEqual(3, GetRowCount($"SELECT COUNT(*) FROM {TableName}"));

            // Update single
            var updateSingle = await TaskRunner.BatchOperation(inputUpdateSingle, options, default);
            Assert.IsTrue(updateSingle.Success);
            Assert.AreEqual(1, updateSingle.RecordsAffected);
            Assert.IsNull(updateSingle.Error);
            Assert.AreEqual(1, GetRowCount($"SELECT COUNT(*) FROM {TableName} WHERE LastName = 'Edit'"));

            // Update multiple
            var updateMultiple = await TaskRunner.BatchOperation(inputUpdateMultiple, options, default);
            Assert.IsTrue(updateMultiple.Success);
            Assert.AreEqual(2, updateMultiple.RecordsAffected);
            Assert.IsNull(updateMultiple.Error);
            Assert.AreEqual(2, GetRowCount($"SELECT COUNT(*) FROM {TableName} WHERE LastName = 'Foobar'"));

            // Delete single
            var deleteSingle = await TaskRunner.BatchOperation(inputDeleteSingle, options, default);
            Assert.IsTrue(deleteSingle.Success);
            Assert.AreEqual(1, deleteSingle.RecordsAffected);
            Assert.IsNull(deleteSingle.Error);
            Assert.AreEqual(2, GetRowCount($"SELECT COUNT(*) FROM {TableName}"));

            // Delete multiple
            var deleteMultiple = await TaskRunner.BatchOperation(inputDeleteMultiple, options, default);
            Assert.IsTrue(deleteMultiple.Success);
            Assert.AreEqual(2, deleteMultiple.RecordsAffected);
            Assert.IsNull(deleteMultiple.Error);
            Assert.AreEqual(0, GetRowCount($"SELECT COUNT(*) FROM {TableName}"));

            CleanUp();
        }
    }

    private static int GetRowCount(string query)
    {
        try
        {
            using var connection = new SqlConnection(ConnString);
            connection.Open();
            var getRows = connection.CreateCommand();
            getRows.CommandText = query;
            var count = (int)getRows.ExecuteScalar();
            connection.Close();
            connection.Dispose();
            return count;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
