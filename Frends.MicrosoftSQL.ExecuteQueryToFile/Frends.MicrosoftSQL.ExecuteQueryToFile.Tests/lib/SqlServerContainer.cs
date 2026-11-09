using System.Threading.Tasks;
using NUnit.Framework;
using Testcontainers.MsSql;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

[SetUpFixture]
public sealed class SqlServerContainer
{
    private const string Image = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04";
    private MsSqlContainer container;

    internal static string ConnectionString { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        container = new MsSqlBuilder(Image).Build();
        await container.StartAsync();
        ConnectionString = container.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (container != null)
            await container.DisposeAsync();
    }
}
