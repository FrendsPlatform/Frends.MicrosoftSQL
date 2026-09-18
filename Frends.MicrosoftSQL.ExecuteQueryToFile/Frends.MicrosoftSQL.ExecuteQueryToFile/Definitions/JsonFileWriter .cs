using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

internal class JsonFileWriter : IAsyncDisposable
{
    internal JsonFileWriter(SqlCommand sqlCommand, Input input, JsonOptions options)
    {
        SqlCommand = sqlCommand;
        Input = input;
        Options = options;
    }

    private SqlCommand SqlCommand { get; }

    private Input Input { get; }

    private JsonOptions Options { get; }

    public async ValueTask DisposeAsync()
    {
        if (SqlCommand != null)
            await SqlCommand.DisposeAsync();
    }

    public async Task<Result> SaveQueryToJson(CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(
            Input.OutputFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: Options.FileBufferSize,
            useAsync: true);

        await using var streamWriter = new StreamWriter(fileStream);

        using var jsonWriter = new JsonTextWriter(streamWriter)
        {
            Formatting = Options.JsonOutputMode == JsonOutputMode.Indented
                ? Formatting.Indented
                : Formatting.None,

            DateFormatHandling = DateFormatHandling.IsoDateFormat,
        };

        using var reader = await SqlCommand
            .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
            .ConfigureAwait(false);

        int count = Options.JsonOutputMode == JsonOutputMode.Indented
            ? await WriteIndented(reader, jsonWriter, cancellationToken).ConfigureAwait(false)
            : await WriteJsonLines(reader, streamWriter, cancellationToken).ConfigureAwait(false);

        await streamWriter.FlushAsync().ConfigureAwait(false);

        return new Result(count, Input.OutputFilePath, Path.GetFileName(Input.OutputFilePath));
    }

    private async Task<int> WriteIndented(
        DbDataReader reader,
        JsonTextWriter jsonWriter,
        CancellationToken cancellationToken)
    {
        var schema = BuildSchema(reader);
        int count = 0;

        await jsonWriter.WriteStartArrayAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            await jsonWriter.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);

            foreach (var col in schema)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await jsonWriter.WritePropertyNameAsync(col.Name, cancellationToken).ConfigureAwait(false);
                await WriteValue(jsonWriter, reader, col, cancellationToken).ConfigureAwait(false);
            }

            await jsonWriter.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
            count++;
        }

        await jsonWriter.WriteEndArrayAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    private async Task<int> WriteJsonLines(
        DbDataReader reader,
        StreamWriter streamWriter,
        CancellationToken cancellationToken)
    {
        var schema = BuildSchema(reader);
        int count = 0;

        using var jsonWriter = new JsonTextWriter(streamWriter)
        {
            Formatting = Formatting.None,
            CloseOutput = false,
        };

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            await jsonWriter.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);

            foreach (var col in schema)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await jsonWriter.WritePropertyNameAsync(col.Name, cancellationToken).ConfigureAwait(false);
                await WriteValue(jsonWriter, reader, col, cancellationToken).ConfigureAwait(false);
            }

            await jsonWriter.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
            await jsonWriter.WriteRawAsync("\n", cancellationToken).ConfigureAwait(false);
            count++;
        }

        return count;
    }

    private async Task WriteValue(
    JsonTextWriter jsonWriter,
    DbDataReader reader,
    ColumnSchema col,
    CancellationToken cancellationToken)
    {
        if (reader.IsDBNull(col.Index))
        {
            if (Options.HandleNullAsEmpty && col.DotnetType == typeof(string))
                await jsonWriter.WriteValueAsync(string.Empty, cancellationToken).ConfigureAwait(false);
            else
                await jsonWriter.WriteNullAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (col.DotnetType == typeof(byte[]))
        {
            await WriteBinaryAsBase64Async(jsonWriter, reader, col.Index, cancellationToken).ConfigureAwait(false);
            return;
        }

        var value = MapValue(reader, col);
        await jsonWriter.WriteValueAsync(value, cancellationToken).ConfigureAwait(false);
    }

    private object MapValue(DbDataReader reader, ColumnSchema col)
    {
        var value = reader.GetValue(col.Index);
        var type = col.DotnetType;

        if (type == typeof(DateTime))
        {
            var fmt = col.DbTypeName == "date" ? Options.DateFormat : Options.DateTimeFormat;
            return ((DateTime)value).ToString(fmt, CultureInfo.InvariantCulture);
        }

        if (type == typeof(DateTimeOffset))
            return ((DateTimeOffset)value).ToString(Options.DateTimeOffsetFormat, CultureInfo.InvariantCulture);

        if (type == typeof(TimeSpan))
            return ((TimeSpan)value).ToString(Options.TimeFormat, CultureInfo.InvariantCulture);

        if (type == typeof(Guid))
            return ((Guid)value).ToString();

        return value;
    }

    /// <summary>
    /// Streams a binary column to JSON as a Base64-encoded string using fixed-size chunks,
    /// avoiding loading the entire value into memory.
    /// </summary>
    private static async Task WriteBinaryAsBase64Async(
        JsonTextWriter jsonWriter,
        DbDataReader reader,
        int columnIndex,
        CancellationToken cancellationToken)
    {
        const int InputChunkSize = 3 * 1024;

        await using var binaryStream = reader.GetStream(columnIndex);

        await jsonWriter.WriteRawValueAsync("\"", cancellationToken).ConfigureAwait(false);

        var inputBuffer = new byte[InputChunkSize];

        var outputBuffer = new char[((InputChunkSize / 3) + 1) * 4];

        int bytesRead;
        while ((bytesRead = await ReadChunkAsync(binaryStream, inputBuffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            int charsWritten = Convert.ToBase64CharArray(
                inputBuffer, 0, bytesRead, outputBuffer, 0);

            await jsonWriter.WriteRawAsync(
                    new string(outputBuffer, 0, charsWritten), cancellationToken)
                .ConfigureAwait(false);
        }

        await jsonWriter.WriteRawAsync("\"", cancellationToken).ConfigureAwait(false);
    }

    private static List<ColumnSchema> BuildSchema(DbDataReader reader)
    {
        var schema = new List<ColumnSchema>(reader.FieldCount);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            schema.Add(new ColumnSchema(
                index: i,
                name: reader.GetName(i),
                dotnetType: reader.GetFieldType(i),
                dbTypeName: reader.GetDataTypeName(i)?.ToLowerInvariant() ?? string.Empty));
        }

        return schema;
    }

    private static async Task<int> ReadChunkAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream
                .ReadAsync(buffer, total, buffer.Length - total, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
        }

        return total;
    }
}
