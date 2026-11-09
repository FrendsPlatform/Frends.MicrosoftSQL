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
    public async Task BulkInsertFailure_RethrowsOriginalSqlException()
    {
        var input = new Input { TableName = "MissingTable", InputData = Json };
        var options = new Options
        {
            ThrowErrorOnFailure = true,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
            NotifyAfter = 1,
        };

        var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
            TaskRunner.BulkInsert(input, options, CancellationToken.None));

        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public async Task BulkInsertFailure_RollsBackRows_ReturnsError()
    {
        await WithFailingTrigger(async () =>
        {
            var result = await TaskRunner.BulkInsert(CreateInput(), CreateRollbackOptions(false), default);

            Assert.IsFalse(result.Success);
            Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(SqlException));
            StringAssert.Contains(result.Error.Message, "rollback test failure");
            Assert.AreEqual(0, GetRowCount());
        });
    }

    [TestMethod]
    public async Task BulkInsertFailure_RollsBackRows_RethrowsOriginalSqlException()
    {
        await WithFailingTrigger(async () =>
        {
            var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
                TaskRunner.BulkInsert(CreateInput(), CreateRollbackOptions(true), default));

            StringAssert.Contains(exception.Message, "rollback test failure");
            Assert.IsNull(exception.InnerException);
            Assert.AreEqual(0, GetRowCount());
        });
    }

    private static Input CreateInput() => new()
    {
        TableName = "TestTable",
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

    private static async Task WithFailingTrigger(Func<Task> test)
    {
        const string triggerName = "FailBulkInsert";
        Execute($@"
            IF OBJECT_ID('{triggerName}', 'TR') IS NOT NULL DROP TRIGGER {triggerName};");
        Execute($@"
            CREATE TRIGGER {triggerName} ON TestTable AFTER INSERT AS
            BEGIN
                THROW 51000, 'rollback test failure', 1;
            END");

        try
        {
            await test();
        }
        finally
        {
            Execute($"DROP TRIGGER IF EXISTS {triggerName}");
        }
    }

    private static int GetRowCount()
    {
        using var sqlConnection = new SqlConnection(SqlServerContainer.ConnectionString);
        sqlConnection.Open();
        using var command = sqlConnection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM TestTable";
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
