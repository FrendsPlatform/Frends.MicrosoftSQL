using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.ExecuteProcedure.Tests;

[TestClass]
public class ExceptionUnitTests
{
    /*
        docker-compose up

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
            Parameters = null
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
        var ex = await Assert.ThrowsExceptionAsync<Exception>(() => MicrosoftSQL.ExecuteProcedure(_input, _options, default));
        Assert.IsTrue(ex.Message.Contains("SqlException (0x80131904): Login failed for user 'SA'."));
    }

    [TestMethod]
    public async Task TestExecuteProcedure_Invalid_Creds_ReturnErrorMessage()
    {
        var options = _options;
        options.ThrowErrorOnFailure = false;

        var result = await MicrosoftSQL.ExecuteProcedure(_input, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("Login failed for user 'SA'."));
        Assert.AreEqual(0, result.RecordsAffected);
        Assert.IsNull(result.Data);
    }
}