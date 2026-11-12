using Frends.MicrosoftSQL.BatchOperation.Definitions;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

internal static class TaskRunner
{
    internal static Task<Result> BatchOperation(
        Input input,
        Options options,
        CancellationToken cancellationToken,
        Connection? connection = null) =>
        MicrosoftSQL.BatchOperation(
            input,
            connection ?? new Connection { ConnectionString = Lib.SqlServerContainer.ConnectionString },
            options,
            cancellationToken);
}
