using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Frends.MicrosoftSQL.BatchOperation.Tests.Lib;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

[TestClass]
public class ExceptionUnitTests
{
    [TestMethod]
    public async Task InvalidCredentials_RethrowsOriginalSqlException()
    {
        var connectionString = new SqlConnectionStringBuilder(SqlServerContainer.ConnectionString)
        {
            Password = "WrongPassWord",
        }.ConnectionString;

        var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
            TaskRunner.BatchOperation(
                new Input(),
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

        var result = await TaskRunner.BatchOperation(
            new Input(),
            new Options { ThrowErrorOnFailure = false },
            default,
            new Connection { ConnectionString = connectionString });

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error.Message, "Login failed for user");
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(SqlException));
    }

    [TestMethod]
    public async Task BatchOperationFailure_RethrowsOriginalSqlExceptionWithoutTransaction()
    {
        var input = new Input
        {
            Query = "INSERT INTO MissingTable VALUES (1)",
            InputJson = "[{}]",
        };
        var options = new Options
        {
            ThrowErrorOnFailure = true,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.None,
        };

        var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
            TaskRunner.BatchOperation(input, options, default));

        StringAssert.Contains(exception.Message, "Invalid object name");
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public async Task BatchOperationFailure_RollsBackAndReturnsOriginalSqlException()
    {
        const string tableName = "RollbackTest";
        PrepareTable(tableName);

        var result = await TaskRunner.BatchOperation(CreateFailingInput(tableName), CreateRollbackOptions(false), default);

        Assert.IsFalse(result.Success);
        Assert.IsInstanceOfType(result.Error.AdditionalInfo, typeof(SqlException));
        StringAssert.Contains(result.Error.Message, "rollback test failure");
        Assert.AreEqual(0, GetRowCount(tableName));
        DropTable(tableName);
    }

    [TestMethod]
    public async Task BatchOperationFailure_RollsBackAndRethrowsOriginalSqlException()
    {
        const string tableName = "RollbackTestThrow";
        PrepareTable(tableName);

        var exception = await Assert.ThrowsExceptionAsync<SqlException>(() =>
            TaskRunner.BatchOperation(CreateFailingInput(tableName), CreateRollbackOptions(true), default));

        StringAssert.Contains(exception.Message, "rollback test failure");
        Assert.IsNull(exception.InnerException);
        Assert.AreEqual(0, GetRowCount(tableName));
        DropTable(tableName);
    }

    private static Input CreateFailingInput(string tableName) => new()
    {
        // Rows 1 and 2 are inserted before row 3 fails, so rollback has to remove real data.
        Query = $"INSERT INTO {tableName} VALUES (@Id); IF @Id = 3 THROW 51000, 'rollback test failure', 1;",
        InputJson = "[{\"Id\":1},{\"Id\":2},{\"Id\":3}]",
    };

    private static Options CreateRollbackOptions(bool throwErrorOnFailure) => new()
    {
        ThrowErrorOnFailure = throwErrorOnFailure,
        SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
        CommandTimeoutSeconds = 30,
    };

    private static void PrepareTable(string tableName) =>
        Execute($"IF OBJECT_ID('{tableName}') IS NOT NULL DROP TABLE {tableName}; CREATE TABLE {tableName}(Id int);");

    private static void DropTable(string tableName) => Execute($"DROP TABLE {tableName}");

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
