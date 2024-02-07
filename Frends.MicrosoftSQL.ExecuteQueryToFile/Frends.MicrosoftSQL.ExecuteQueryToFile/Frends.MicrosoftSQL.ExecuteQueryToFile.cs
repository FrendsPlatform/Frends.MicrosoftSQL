namespace Frends.MicrosoftSQL.ExecuteQueryToFile;

using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

/// <summary>
/// Main class of the Task.
/// </summary>
public static class MicrosoftSQL
{
    /// <summary>
    /// Frends Task for executing Microsoft SQL queries into a file.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.MicrosoftSQL.ExecuteQueryToFile).
    /// </summary>
    /// <param name="input">Input parameters.</param>
    /// <param name="options">Options parameters.</param>
    /// <param name="cancellationToken">Cancellation token given by Frends.</param>
    /// <returns>Object { int EntriesWritten, string Path, string FileName }</returns>
    public static async Task<Result> ExecuteQueryToFile([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
    {
        Result result = new();
        using (var sqlConnection = new SqlConnection(input.ConnectionString))
        {
            await sqlConnection.OpenAsync(cancellationToken);
            using var command = BuildSQLCommand(input.Query, input.QueryParameters);
            command.CommandTimeout = options.TimeoutSeconds;
            command.Connection = sqlConnection;

            switch (options.ReturnFormat)
            {
                case ReturnFormat.CSV:
                    var csvWriter = new CsvFileWriter(command, input, options.CsvOptions);
                    result = await csvWriter.SaveQueryToCSV(cancellationToken);
                    break;
            }
        }

        return result;
    }

    private static SqlCommand BuildSQLCommand(string query, Definitions.SqlParameter[] parmeters)
    {
        using var command = new SqlCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

        foreach (var parameter in parmeters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return command;
    }
}
