using Frends.MicrosoftSQL.BulkInsert.Definitions;
using Frends.MicrosoftSQL.BulkInsert.Tests.Lib;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

[TestClass]
public class ExceptionUnitTests
{
    private const string Json = """[{"Id":1,"Firstname":"Etu","Lastname":"Suku"}]""";

    [TestMethod]
    public async Task InvalidCredentials_RethrowsOriginalSqlException()
    {
        var connectionString = new SqlConnectionStringBuilder(SqlServerContainer.ConnectionString)
        {
            Password = "WrongPassWord",
        }.ConnectionString;

        var input = new Input { TableName = "TestTable", InputData = Json };
        var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
            TaskRunner.BulkInsert(
                input,
                new Options { ThrowErrorOnFailure = true },
                default,
                new Connection { ConnectionString = connectionString }));

        StringAssert.Contains(exception.Message, "Login failed for user");
    }

    [TestMethod]
    public async Task InvalidCredentials_ReturnsErrorWhenThrowingDisabled()
    {
        var connectionString = new SqlConnectionStringBuilder(SqlServerContainer.ConnectionString)
        {
            Password = "WrongPassWord",
        }.ConnectionString;

        var result = await TaskRunner.BulkInsert(
            new Input { TableName = "TestTable", InputData = Json },
            new Options { ThrowErrorOnFailure = false },
            default,
            new Connection { ConnectionString = connectionString });

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Login failed for user");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(SqlException));
    }

    [TestMethod]
    public void BulkInsertFailure_RethrowsOriginalException()
    {
        var input = new Input { TableName = "MissingTable", InputData = Json };
        var options = new Options
        {
            ThrowErrorOnFailure = true,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            NotifyAfter = 1,
        };

        Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            TaskRunner.BulkInsert(input, options, CancellationToken.None));
    }

    [TestMethod]
    public async Task BulkInsertFailure_RollsBackRows_ReturnsError()
    {
        await WithFailingTrigger(async tableName =>
        {
            var result = await TaskRunner.BulkInsert(CreateInput(tableName), CreateRollbackOptions(false), default);

            Assert.IsFalse(result.Success);
            Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(SqlException));
            StringAssert.Contains(result.Error.Message, "rollback test failure");
            Assert.AreEqual(0, GetRowCount(tableName));
        });
    }

    [TestMethod]
    public async Task BulkInsertFailure_RollsBackRows_RethrowsOriginalSqlException()
    {
        await WithFailingTrigger(async tableName =>
        {
            var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
                TaskRunner.BulkInsert(CreateInput(tableName), CreateRollbackOptions(true), default));

            StringAssert.Contains(exception.Message, "rollback test failure");
            Assert.IsNull(exception.InnerException);
            Assert.AreEqual(0, GetRowCount(tableName));
        });
    }

    private static Input CreateInput(string tableName) => new()
    {
        TableName = tableName,
        InputData = """
            [
              {"Id":1,"Firstname":"Etu","Lastname":"Suku"},
              {"Id":2,"Firstname":"Suku","Lastname":"Etu"},
              {"Id":3,"Firstname":"F?rst","Lastname":"L??st"}
            ]
            """,
    };

    private static Options CreateRollbackOptions(bool throwErrorOnFailure) => new()
    {
        ThrowErrorOnFailure = throwErrorOnFailure,
        FireTriggers = true,
        SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
        CommandTimeoutSeconds = 30,
    };

    private static async Task WithFailingTrigger(Func<string, Task> test)
    {
        var tableName = $"BulkInsertRollback_{Guid.NewGuid():N}";
        var triggerName = $"FailBulkInsert_{Guid.NewGuid():N}";
        Execute($@"
            CREATE TABLE {tableName} (Id int, Firstname varchar(255), Lastname varchar(255));");
        Execute($@"
            CREATE TRIGGER {triggerName} ON {tableName} AFTER INSERT AS
            BEGIN
                THROW 51000, 'rollback test failure', 1;
            END");

        try
        {
            await test(tableName);
        }
        finally
        {
            Execute($"DROP TRIGGER IF EXISTS {triggerName}; DROP TABLE IF EXISTS {tableName};");
        }
    }

    private static int GetRowCount(string tableName)
    {
        using var sqlConnection = new SqlConnection(SqlServerContainer.ConnectionString);
        sqlConnection.Open();
        using var command = sqlConnection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return (int)command.ExecuteScalar();
    }

    private static void Execute(string sql)
    {
        using var sqlConnection = new SqlConnection(SqlServerContainer.ConnectionString);
        sqlConnection.Open();
        using var command = sqlConnection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
