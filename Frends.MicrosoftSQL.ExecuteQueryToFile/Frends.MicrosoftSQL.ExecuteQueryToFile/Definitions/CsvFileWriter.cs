using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;

internal class CsvFileWriter
{
    internal CsvFileWriter(SqlCommand sqlCommand, Input input, CsvOptions options)
    {
        SqlCommand = sqlCommand;
        Input = input;
        Options = options;
    }

    private SqlCommand SqlCommand { get; set; }

    private Input Input { get; set; }

    private CsvOptions Options { get; set; }

    public async Task<Result> SaveQueryToCSV(CancellationToken cancellationToken)
    {
        var output = 0;
        var encoding = GetEncoding(Options.FileEncoding, Options.EnableBom, Options.EncodingInString);

        using (var writer = new StreamWriter(Input.OutputFilePath, false, encoding))
        using (var csvFile = CreateCsvWriter(Options.GetFieldDelimiterAsString(), writer))
        {
            writer.NewLine = Options.GetLineBreakAsString();

            var reader = await SqlCommand.ExecuteReaderAsync(cancellationToken);
            output = DataReaderToCsv(reader, csvFile, Options, cancellationToken);

            csvFile.Flush();
        }

        return new Result(output, Input.OutputFilePath, Path.GetFileName(Input.OutputFilePath));
    }

    private static CsvWriter CreateCsvWriter(string delimiter, TextWriter writer)
    {
        var csvOptions = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
        };

        return new CsvWriter(writer, csvOptions);
    }

    private static string FormatDbHeader(string header, bool forceSpecialFormatting)
    {
        if (!forceSpecialFormatting) return header;

        // First part of regex removes all non-alphanumeric ('_' also allowed) chars from the whole string.
        // Second part removed any leading numbers or underscoress.
        var rgx = new Regex("[^a-zA-Z0-9_-]|^[0-9_]+");
        header = rgx.Replace(header, string.Empty);
        return header.ToLower();
    }

    private static string FormatDbValue(object value, string dbTypeName, Type dotnetType, CsvOptions options)
    {
        if (value == null || value == DBNull.Value)
        {
            if (dotnetType == typeof(string)) return "\"\"";
            if (dotnetType == typeof(DateTime) && options.AddQuotesToDates) return "\"\"";
            return string.Empty;
        }

        if (dotnetType == typeof(string))
        {
            var str = (string)value;
            options.GetFieldDelimiterAsString();
            str = str.Replace("\"", "\\\"");
            str = str.Replace("\r\n", " ");
            str = str.Replace("\r", " ");
            str = str.Replace("\n", " ");
            if (options.AddQuotesToStrings)
                return $"\"{str}\"";
            return str;
        }

        if (dotnetType == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            var dbType = dbTypeName?.ToLower();
            string output = dbType switch
            {
                "date" => dateTime.ToString(options.DateFormat, CultureInfo.InvariantCulture),
                _ => dateTime.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture),
            };
            if (options.AddQuotesToDates) return $"\"{output}\"";
            return output;
        }

        if (dotnetType == typeof(float))
            return ((float)value).ToString("0.###########", CultureInfo.InvariantCulture);

        if (dotnetType == typeof(double))
            return ((double)value).ToString("0.###########", CultureInfo.InvariantCulture);

        if (dotnetType == typeof(decimal))
            return ((decimal)value).ToString("0.###########", CultureInfo.InvariantCulture);

        if (dotnetType == typeof(byte[]))
            return BitConverter.ToString((byte[])value);

        return value.ToString();
    }

    private static int DataReaderToCsv(
        DbDataReader reader,
        CsvWriter csvWriter,
        CsvOptions options,
        CancellationToken cancellationToken)
    {
        // Write header and remember column indexes to include.
        var columnIndexesToInclude = new List<int>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var includeColumn =
                options.ColumnsToInclude == null ||
                options.ColumnsToInclude.Length == 0 ||
                options.ColumnsToInclude.Contains(columnName);

            if (includeColumn)
            {
                if (options.IncludeHeadersInOutput)
                {
                    var formattedHeader = FormatDbHeader(columnName, options.SanitizeColumnHeaders);
                    csvWriter.WriteField(formattedHeader);
                }

                columnIndexesToInclude.Add(i);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (options.IncludeHeadersInOutput) csvWriter.NextRecord();

        int count = 0;
        while (reader.Read())
        {
            foreach (var columnIndex in columnIndexesToInclude)
            {
                var value = reader.GetValue(columnIndex);
                var dbTypeName = reader.GetDataTypeName(columnIndex);
                var dotnetType = reader.GetFieldType(columnIndex);
                var formattedValue = FormatDbValue(value, dbTypeName, dotnetType, options);
                csvWriter.WriteField(formattedValue, false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            csvWriter.NextRecord();
            count++;
        }

        return count;
    }

    private static Encoding GetEncoding(FileEncoding optionsFileEncoding, bool optionsEnableBom, string optionsEncodingInString)
    {
        return optionsFileEncoding switch
        {
            FileEncoding.Other => Encoding.GetEncoding(optionsEncodingInString),
            FileEncoding.ASCII => Encoding.ASCII,
            FileEncoding.ANSI => Encoding.Default,
            FileEncoding.UTF8 => optionsEnableBom ? new UTF8Encoding(true) : new UTF8Encoding(false),
            FileEncoding.Unicode => Encoding.Unicode,
            _ => Encoding.ASCII,
        };
    }
}
