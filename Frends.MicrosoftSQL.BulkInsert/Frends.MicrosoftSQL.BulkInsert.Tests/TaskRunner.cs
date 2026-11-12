using Frends.MicrosoftSQL.BulkInsert.Definitions;

namespace Frends.MicrosoftSQL.BulkInsert.Tests;

internal static class TaskRunner
{
    internal static Task<Result> BulkInsert(
        Input input,
        Options options,
        CancellationToken cancellationToken,
        Connection? connection = null) =>
        MicrosoftSQL.BulkInsert(
            input,
            connection ?? new Connection { ConnectionString = Lib.SqlServerContainer.ConnectionString },
            options,
            cancellationToken);
}
