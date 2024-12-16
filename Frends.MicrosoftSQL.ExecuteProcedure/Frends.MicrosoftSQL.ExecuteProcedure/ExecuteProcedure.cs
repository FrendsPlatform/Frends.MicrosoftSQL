﻿using Frends.MicrosoftSQL.ExecuteProcedure.Definitions;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using IsolationLevel = System.Data.IsolationLevel;
using Microsoft.SqlServer.Types;

namespace Frends.MicrosoftSQL.ExecuteProcedure;

/// <summary>
/// MicrosoftSQL Task.
/// </summary>
public class MicrosoftSQL
{
    /// <summary>
    /// Execute Microsoft SQL Server procedure.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.MicrosoftSQL.ExecuteProcedure)
    /// </summary>
    /// <param name="input">Input parameters</param>
    /// <param name="options">Optional parameters</param>
    /// <param name="cancellationToken">Token generated by Frends to stop this Task.</param>
    /// <returns>Object { bool Success, int RecordsAffected, string ErrorMessage, JToken Data }</returns>
    public static async Task<Result> ExecuteProcedure([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
    {
        Result result;

        using var connection = new SqlConnection(input.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandTimeout = options.CommandTimeoutSeconds;
            command.CommandText = input.Execute;
            command.CommandType = CommandType.StoredProcedure;

            if (input.Parameters != null)
            {
                foreach (var parameter in input.Parameters)
                {
                    if (parameter.Value is null)
                        parameter.Value = DBNull.Value;

                    if (parameter.SqlDataType is SqlDataTypes.Auto)
                        command.Parameters.AddWithValue(parameterName: parameter.Name, value: parameter.Value);
                    else
                    {
                        var sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), parameter.SqlDataType.ToString());
                        var commandParameter = command.Parameters.Add(parameter.Name, sqlDbType);
                        commandParameter.Value = parameter.Value;
                    }
                }
            }

            if (options.SqlTransactionIsolationLevel is SqlTransactionIsolationLevel.None)
                result = await ExecuteHandler(input, options, command, cancellationToken);
            else
            {
                using var transaction = connection.BeginTransaction(GetIsolationLevel(options.SqlTransactionIsolationLevel));
                command.Transaction = transaction;
                result = await ExecuteHandler(input, options, command, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return HandleExecutionException(ex, options);
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
    }

    private static async Task<Result> ExecuteHandler(Input input, Options options, SqlCommand command, CancellationToken cancellationToken)
    {
        Result result;
        object dataObject;
        SqlDataReader dataReader = null;
        using var table = new DataTable();

        try
        {
            switch (input.ExecuteType)
            {
                case ExecuteTypes.NonQuery:
                    dataObject = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    result = new Result(true, (int)dataObject, null, JToken.FromObject(new { AffectedRows = dataObject }));
                    break;
                case ExecuteTypes.Scalar:
                    dataObject = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                    // JToken.FromObject() method can't handle SqlGeography typed objects so we convert it into string.
                    if (dataObject != null && (dataObject.GetType() == typeof(SqlGeography) || dataObject.GetType() == typeof(SqlGeometry)))
                        dataObject = dataObject.ToString();

                    result = new Result(true, 1, null, JToken.FromObject(new { Value = dataObject }));
                    break;
                case ExecuteTypes.ExecuteReader:
                    dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    result = new Result(true, dataReader.RecordsAffected, null, await LoadData(dataReader, cancellationToken));
                    await dataReader.CloseAsync();
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (command.Transaction != null)
                await command.Transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            if (dataReader != null && !dataReader.IsClosed)
                await dataReader.CloseAsync();

            return HandleExecutionException(ex, options, command);
        }
    }

    private static async Task<JToken> LoadData(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var table = new JArray();
        while (reader.HasRows)
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new JObject();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    object fieldValue = reader.GetValue(i);
                    object value;
                    if (fieldValue == DBNull.Value)
                        value = null;
                    else if (fieldValue is SqlGeography geography)
                        value = geography.ToString();
                    else if (fieldValue is SqlGeometry geometry)
                        value = geometry.ToString();
                    else
                        value = fieldValue;

                    row.Add(new JProperty(reader.GetName(i), value));
                }

                table.Add(row);
            }
            await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
        }

        return table;
    }

    [ExcludeFromCodeCoverage(Justification = "Requires manual tests to fully test these (see ManualTesting.cs).")]
    private static Result HandleExecutionException(Exception ex, Options options, SqlCommand command = null)
    {
        var eMsg = $"ExecuteProcedure exception: {ex}.";

        if (command == null || command.Transaction == null)
        {
            if (options.ThrowErrorOnFailure)
                throw new Exception(eMsg);
            else
                return new Result(false, 0, eMsg, null);
        }
        else
        {
            try
            {
                command.Transaction.Rollback();
            }
            catch (Exception rollbackEx)
            {
                var rollbackErrorMsg = $"An exception occurred on transaction rollback. Rollback exception: {rollbackEx}.";

                if (options.ThrowErrorOnFailure)
                    throw new Exception(rollbackErrorMsg, rollbackEx);
                else
                    return new Result(false, 0, rollbackErrorMsg, null);
            }

            var rollbackCompletedMsg = "(If required) transaction rollback completed without exception.";
            eMsg += $" || {rollbackCompletedMsg}";

            if (options.ThrowErrorOnFailure)
                throw new Exception(rollbackCompletedMsg, ex);
            else
                return new Result(false, 0, eMsg, null);
        }
    }

    private static IsolationLevel GetIsolationLevel(SqlTransactionIsolationLevel sqlTransactionIsolationLevel)
    {
        return sqlTransactionIsolationLevel switch
        {
            SqlTransactionIsolationLevel.Unspecified => IsolationLevel.Unspecified,
            SqlTransactionIsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
            SqlTransactionIsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
            SqlTransactionIsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
            SqlTransactionIsolationLevel.Serializable => IsolationLevel.Serializable,
            SqlTransactionIsolationLevel.Snapshot => IsolationLevel.Snapshot,
            _ => IsolationLevel.ReadCommitted,
        };
    }
}