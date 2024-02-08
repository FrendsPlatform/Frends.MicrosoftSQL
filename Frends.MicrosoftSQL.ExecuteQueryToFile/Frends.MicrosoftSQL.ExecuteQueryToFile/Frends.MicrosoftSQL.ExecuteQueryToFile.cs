namespace Frends.MicrosoftSQL.ExecuteQueryToFile;

using System;
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

            using var command = sqlConnection.CreateCommand();
            command.CommandTimeout = options.TimeoutSeconds;
            command.CommandText = input.Query;
            command.CommandType = CommandType.Text;

            if (input.QueryParameters != null)
            {
                foreach (var parameter in input.QueryParameters)
                {
                    if (parameter.SqlDataType is SqlDataTypes.Auto)
                    {
                        command.Parameters.AddWithValue(parameterName: parameter.Name, value: parameter.Value);
                    }
                    else
                    {
                        var sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), parameter.SqlDataType.ToString());
                        var commandParameter = command.Parameters.Add(parameter.Name, sqlDbType);
                        commandParameter.Value = parameter.Value;
                    }
                }
            }

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
}
