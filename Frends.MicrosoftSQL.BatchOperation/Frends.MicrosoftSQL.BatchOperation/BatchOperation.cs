﻿using Dapper;
using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using IsolationLevel = System.Data.IsolationLevel;

namespace Frends.MicrosoftSQL.BatchOperation;

/// <summary>
/// MicrosoftSQL Task.
/// </summary>
public class MicrosoftSQL
{
    /// Mem cleanup.
    static MicrosoftSQL()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        var currentContext = AssemblyLoadContext.GetLoadContext(currentAssembly);
        if (currentContext != null)
            currentContext.Unloading += OnPluginUnloadingRequested;
    }

    /// <summary>
    /// Use JSON array to create and execute the query as a Microsoft SQL Server batch operation.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.MicrosoftSQL.BatchOperation)
    /// </summary>
    /// <param name="input">Input parameters</param>
    /// <param name="options">Optional parameters</param>
    /// <param name="cancellationToken">Token generated by Frends to stop this Task.</param>
    /// <returns>Object { bool Success, int RecordsAffected, string ErrorMessage, JToken Data }</returns>
    public static async Task<Result> BatchOperation([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SqlConnection(input.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            if (options.SqlTransactionIsolationLevel is not SqlTransactionIsolationLevel.None)
            {
                using var transaction = connection.BeginTransaction(GetIsolationLevel(options));
                return await ExecuteHandler(input, options, connection, transaction, cancellationToken);
            }
            else
                return await ExecuteHandler(input, options, connection, null, cancellationToken);
        }
        catch (Exception ex)
        {
            if (options.ThrowErrorOnFailure)
                throw new Exception($"BatchOperation exception: ", ex);

            return new Result(false, 0, ex.Message);
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
    }

    private static async Task<Result> ExecuteHandler(Input input, Options options, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        var affectedRows = 0;
        try
        {
            var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(input.InputJson, new ExpandoObjectConverter());

            affectedRows = await connection.ExecuteAsync(
                                input.Query,
                                param: obj,
                                commandTimeout: options.CommandTimeoutSeconds,
                                commandType: CommandType.Text,
                                transaction: transaction)
                            .ConfigureAwait(false);
            
            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            return new Result(true, affectedRows, null);
        }
        catch (Exception ex)
        {
            if (transaction is null)
            {
                if (options.ThrowErrorOnFailure)
                    throw new Exception("ExecuteHandler exception: 'Options.SqlTransactionIsolationLevel = None', so there was no transaction rollback.", ex);
                else
                    return new Result(false, affectedRows, $"ExecuteHandler exception: 'Options.SqlTransactionIsolationLevel = None', so there was no transaction rollback. {ex}");
            }
            else
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    if (options.ThrowErrorOnFailure)
                        throw new Exception("ExecuteHandler exception: An exception occurred on transaction rollback.", rollbackEx);
                    else
                        return new Result(false, affectedRows, $"ExecuteHandler exception: An exception occurred on transaction rollback. Rollback exception: {rollbackEx}. ||  Exception leading to rollback: {ex}");
                }

                if (options.ThrowErrorOnFailure)
                    throw new Exception("ExecuteHandler exception: (If required) transaction rollback completed without exception.", ex);
                else
                    return new Result(false, affectedRows, $"ExecuteHandler exception: (If required) transaction rollback completed without exception. {ex}.");
            }
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
    }

    private static IsolationLevel GetIsolationLevel(Options options)
    {
        return options.SqlTransactionIsolationLevel switch
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

    private static void OnPluginUnloadingRequested(AssemblyLoadContext obj)
    {
        obj.Unloading -= OnPluginUnloadingRequested;
    }
}