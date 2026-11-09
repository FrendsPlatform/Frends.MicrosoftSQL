using System;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Helpers;
using Microsoft.Data.SqlClient;
using SimpleImpersonation;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile;

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
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Options parameters.</param>
    /// <param name="cancellationToken">Cancellation token given by Frends.</param>
    /// <returns>object { bool Success, int EntriesWritten, string Path, string FileName, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> ExecuteQueryToFile(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(connection);
            if (connection.UseGivenUserCredentialsForRemoteConnections)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    throw new PlatformNotSupportedException("Simple impersonation feature is only supported on Windows machines.");

                var domainAndUsername = (connection.Username ?? string.Empty).Split('\\');
                if (domainAndUsername.Length != 2)
                    throw new ArgumentException($@"Username field must be of format domain\username was: {connection.Username}");

                var credentials = new UserCredentials(domainAndUsername[0], domainAndUsername[1], connection.Password);
                using var handle = credentials.LogonUser(LogonType.NewCredentials);
                return await WindowsIdentity.RunImpersonatedAsync(
                        handle,
                        () => ExecuteQueryToFileInternal(input, connection, options, cancellationToken))
                    .ConfigureAwait(false);
            }

            return await ExecuteQueryToFileInternal(input, connection, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }

    private static async Task<Result> ExecuteQueryToFileInternal(
        Input input,
        Connection connection,
        Options options,
        CancellationToken cancellationToken)
    {
        Result result = new();

        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = sqlConnection.CreateCommand();
        command.CommandTimeout = options.TimeoutSeconds;
        command.CommandText = input.Query;
        command.CommandType = CommandType.Text;

        if (input.QueryParameters != null)
        {
            foreach (var parameter in input.QueryParameters)
            {
                parameter.Value ??= DBNull.Value;

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
                {
                    await using var csvWriter = new CsvFileWriter(command, input, options.CsvOptions);
                    result = await csvWriter.SaveQueryToCsv(cancellationToken).ConfigureAwait(false);

                    break;
                }

            case ReturnFormat.JSON:
                {
                    await using var jsonWriter = new JsonFileWriter(command, input, options.JsonOptions);
                    result = await jsonWriter.SaveQueryToJson(cancellationToken).ConfigureAwait(false);
                    break;
                }
        }

        return result;
    }
}
