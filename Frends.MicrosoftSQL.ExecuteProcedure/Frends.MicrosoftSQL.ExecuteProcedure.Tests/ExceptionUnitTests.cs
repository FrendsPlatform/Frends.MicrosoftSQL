using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class ExceptionUnitTests
{
    /*
        docker-compose up -d

        How to use via terminal:
        docker exec -it sql1 "bash"
        /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
        SELECT * FROM TestTable
        GO
   */

    Input _input = new();
    Options _options = new();

    [TestInitialize]
    public void Init()
    {
        _input = new()
        {
            ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=WrongPassWord",
            Execute = "foo",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        _options = new()
        {
            CommandTimeoutSeconds = 2,
            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted,
            ThrowErrorOnFailure = true,
        };
    }

    [TestMethod]
    public async Task TestExecuteProcedure_Invalid_Creds_ThrowError()
    {
        var isolationLevel = Enum.GetValues(typeof(SqlTransactionIsolationLevel)).Cast<SqlTransactionIsolationLevel>().ToList();
        var executeTypes = Enum.GetValues(typeof(ExecuteTypes)).Cast<ExecuteTypes>().ToList();

        foreach (var executeType in executeTypes)
        {
            foreach (var isolation in isolationLevel)
            {
                var input = _input;
                input.ExecuteType = executeType;

                var options = _options;
                options.SqlTransactionIsolationLevel = isolation;

                var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.ExecuteProcedure(input, options, default));
                Assert.IsTrue(ex.Message.Contains("SqlException (0x80131904): Login failed for user 'SA'."), $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
            }
        }
    }

    [TestMethod]
    public async Task TestExecuteProcedure_Invalid_Creds_ReturnErrorMessage()
    {
        var isolationLevel = Enum.GetValues(typeof(SqlTransactionIsolationLevel)).Cast<SqlTransactionIsolationLevel>().ToList();
        var executeTypes = Enum.GetValues(typeof(ExecuteTypes)).Cast<ExecuteTypes>().ToList();

        foreach (var executeType in executeTypes)
        {
            foreach (var isolation in isolationLevel)
            {
                var input = _input;
                input.ExecuteType = executeType;

                var options = _options;
                options.SqlTransactionIsolationLevel = isolation;
                options.ThrowErrorOnFailure = false;

                var result = await MicrosoftSQL.ExecuteProcedure(input, options, default);
                Assert.IsFalse(result.Success, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."), $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.AreEqual(0, result.RecordsAffected, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.IsNull(result.Data, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
            }
        }
    }
}