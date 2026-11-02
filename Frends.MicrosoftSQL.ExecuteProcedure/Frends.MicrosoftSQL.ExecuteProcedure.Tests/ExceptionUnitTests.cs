using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

/// <summary>
/// docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
/// with Git bash add winpty to the start of
/// winpty docker exec -it sql1 "bash"
/// /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
/// Check rows before CleanUp:
/// SELECT* FROM TestTable
/// GO
/// Optional queries:
/// SELECT Name FROM sys.Databases;
/// GO
/// SELECT* FROM INFORMATION_SCHEMA.TABLES;
/// GO
/// </summary>
[TestClass]
public class ExceptionUnitTests
{
    private static readonly string ConnectionString = Helper.GetInvalidConnectionString();

    private Input input = new();
    private Options options = new();

    [TestInitialize]
    public void Init()
    {
        input = new()
        {
            ConnectionString = ConnectionString,
            Execute = "foo",
            ExecuteType = ExecuteTypes.NonQuery,
            Parameters = null,
        };

        options = new()
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
                var procedureInput = input;
                procedureInput.ExecuteType = executeType;

                var procedureOptions = options;
                procedureOptions.SqlTransactionIsolationLevel = isolation;

                var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.ExecuteProcedure(procedureInput, procedureOptions, default));
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
                var procedureInput = input;
                procedureInput.ExecuteType = executeType;

                var procedureOptions = options;
                procedureOptions.SqlTransactionIsolationLevel = isolation;
                procedureOptions.ThrowErrorOnFailure = false;

                var result = await MicrosoftSQL.ExecuteProcedure(procedureInput, procedureOptions, default);
                Assert.IsFalse(result.Success, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."), $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.AreEqual(result.ErrorMessage, result.Error.Message, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.IsNotNull(result.Error.AdditionalInfo, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.AreEqual(0, result.RecordsAffected, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.IsNull(result.Data, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
            }
        }
    }

    [TestMethod]
    public async Task TestExecuteProcedure_CustomErrorMessage()
    {
        options.ThrowErrorOnFailure = false;
        options.ErrorMessageOnFailure = "Custom procedure failure";

        var result = await MicrosoftSQL.ExecuteProcedure(input, options, default);

        Assert.IsTrue(result.Error.Message.StartsWith("Custom procedure failure:"));
        Assert.AreEqual(result.Error.Message, result.ErrorMessage);
    }

    [TestMethod]
    public async Task TestExecuteProcedure_CancellationIsRethrown()
    {
        options.ThrowErrorOnFailure = false;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        try
        {
            await MicrosoftSQL.ExecuteProcedure(input, options, cancellationTokenSource.Token);
            Assert.Fail("Expected cancellation to be rethrown.");
        }
        catch (OperationCanceledException)
        {
        }
    }
}
