using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class ExceptionUnitTests
{
    private Input input = new();
    private Options options = new();

    private static string ConnectionString => Helper.GetInvalidConnectionString();

    [TestInitialize]
    public void Init()
    {
        input = new()
        {
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

                var ex = await Assert.ThrowsExceptionAsync<SqlException>(() =>
                    Helper.ExecuteProcedure(
                        procedureInput,
                        procedureOptions,
                        default,
                        new Connection { ConnectionString = ConnectionString }));
                Assert.IsTrue(ex.Message.Contains("Login failed for user 'SA'."), $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
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

                var result = await Helper.ExecuteProcedure(
                    procedureInput,
                    procedureOptions,
                    default,
                    new Connection { ConnectionString = ConnectionString });
                Assert.IsFalse(result.Success, $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
                Assert.IsTrue(result.Error.Message.Contains("Login failed for user 'SA'."), $"ExecuteType: {executeType}, IsolationLevel: {isolation}");
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

        var result = await Helper.ExecuteProcedure(input, options, default);

        Assert.IsTrue(result.Error.Message.StartsWith("Custom procedure failure:"));
    }

    [TestMethod]
    public async Task TestExecuteProcedure_CancellationIsRethrown()
    {
        options.ThrowErrorOnFailure = false;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        try
        {
            await Helper.ExecuteProcedure(input, options, cancellationTokenSource.Token);
            Assert.Fail("Expected cancellation to be rethrown.");
        }
        catch (OperationCanceledException)
        {
        }
    }
}
