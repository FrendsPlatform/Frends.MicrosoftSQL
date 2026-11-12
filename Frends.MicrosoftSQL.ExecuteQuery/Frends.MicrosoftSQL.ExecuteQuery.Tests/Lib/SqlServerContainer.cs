using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.MsSql;

namespace Frends.MicrosoftSQL.ExecuteQuery.Tests.Lib;

/// <summary>
/// Starts a single SQL Server container for the whole test run and removes it afterwards.
/// </summary>
[TestClass]
public static class SqlServerContainer
{
    private const string Image = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04";

    private static MsSqlContainer? container;

    internal static string ConnectionString { get; private set; } = string.Empty;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext context)
    {
        container = new MsSqlBuilder(Image).Build();

        await container.StartAsync();
        ConnectionString = container.GetConnectionString();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (container != null)
            await container.DisposeAsync();
    }
}
