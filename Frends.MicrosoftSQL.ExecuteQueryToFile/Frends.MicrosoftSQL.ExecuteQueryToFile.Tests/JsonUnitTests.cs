using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

[TestFixture]
public class JsonUnitTests
{
    private static readonly string _connString = Helper.GetConnectionString();
    private static readonly string _tableName = "TestTable";
    private static readonly string _destination = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestData/test.json");

    private Options _options;

    [SetUp]
    public void Init()
    {
        Helper.ExecuteNonQuery(_connString, $"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') DROP TABLE {_tableName}");

        _options = new Options
        {
            TimeoutSeconds = 30,
            ReturnFormat = ReturnFormat.JSON,
            JsonOptions = new JsonOptions
            {
                JsonOutputMode = JsonOutputMode.Indented,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff",
                TimeFormat = @"hh\:mm\:ss\.fff",
                HandleNullAsEmpty = false,
                FileBufferSize = 65536,
            },
        };

        Helper.CreateTestTable(_connString, _tableName);

        var parameters = new Microsoft.Data.SqlClient.SqlParameter[]
        {
        new Microsoft.Data.SqlClient.SqlParameter("@Hash", SqlDbType.VarBinary)
        {
            Value = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_image.png")),
        },
        new Microsoft.Data.SqlClient.SqlParameter("@TestText", SqlDbType.VarBinary)
        {
            Value = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_text.txt")),
        },
        };

        Helper.ExecuteNonQuery(_connString, $"Insert into {_tableName} (Id, LastName, FirstName, Salary, Image, TestText) values (1,'Meikalainen','Matti',1523.25, {parameters[0].ParameterName}, {parameters[1].ParameterName});", parameters);
    }

    [TearDown]
    public void CleanUp()
    {
        using var connection = new SqlConnection(_connString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = $"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN DROP TABLE IF EXISTS {_tableName}; END";
        cmd.ExecuteNonQuery();

        File.Delete(_destination);
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_Indented_WritesValidJsonArray()
    {
        var input = new Input
        {
            Query = $"SELECT Id, LastName, FirstName, Salary FROM {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        var result = await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

        var content = await File.ReadAllTextAsync(_destination);
        var array = JArray.Parse(content); // throws if invalid JSON

        Assert.AreEqual(1, result.EntriesWritten);
        Assert.AreEqual(1, array.Count);
        Assert.AreEqual(1, array[0]["Id"].Value<int>());
        Assert.AreEqual("Meikalainen", array[0]["LastName"].Value<string>());
        Assert.AreEqual("Matti", array[0]["FirstName"].Value<string>());
        Assert.AreEqual(1523.25m, array[0]["Salary"].Value<decimal>());
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_JsonLines_EachRowOnSeparateLine()
    {
        _options.JsonOptions.JsonOutputMode = JsonOutputMode.JsonLines;

        var input = new Input
        {
            Query = $"SELECT Id, LastName, FirstName, Salary FROM {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

        var lines = (await File.ReadAllLinesAsync(_destination))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        Assert.AreEqual(1, lines.Length);

        // Each line must be a valid, self-contained JSON object.
        var row = JObject.Parse(lines[0]);
        Assert.AreEqual(1, row["Id"].Value<int>());
        Assert.AreEqual("Meikalainen", row["LastName"].Value<string>());
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_DataTypes_NumbersBooleansDatesWrittenCorrectly()
    {
        var table = "DataTypeTest";
        var createSql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}')
                BEGIN
                    CREATE TABLE {table} (
                        IntCol      int,
                        BigIntCol   bigint,
                        DecimalCol  decimal(18,4),
                        BitCol      bit,
                        DateCol     date,
                        DateTimeCol datetime2,
                        GuidCol     uniqueidentifier
                    );
                END";

        Helper.CreateTestTable(_connString, table, createSql);

        var guid = Guid.NewGuid();
        var insertSql = $@"
                INSERT INTO {table} VALUES (
                    42, 9999999999, 123.4567, 1,
                    '2024-06-15', '2024-06-15T13:45:00.123',
                    '{guid}'
                )";

        Helper.ExecuteNonQuery(_connString, insertSql);

        var input = new Input
        {
            Query = $"SELECT * FROM {table}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        try
        {
            await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

            var content = await File.ReadAllTextAsync(_destination);
            using var stringReader = new StringReader(content);
            using var jsonReader = new JsonTextReader(stringReader) { DateParseHandling = DateParseHandling.None };
            var array = JArray.Load(jsonReader);
            var row = array[0];

            // Numbers written as numbers, not strings.
            Assert.AreEqual(JTokenType.Integer, row["IntCol"].Type);
            Assert.AreEqual(42, row["IntCol"].Value<int>());
            Assert.AreEqual(JTokenType.Integer, row["BigIntCol"].Type);
            Assert.AreEqual(JTokenType.Float, row["DecimalCol"].Type);
            Assert.AreEqual(123.4567m, row["DecimalCol"].Value<decimal>());

            // Bit written as boolean.
            Assert.AreEqual(JTokenType.Boolean, row["BitCol"].Type);
            Assert.IsTrue(row["BitCol"].Value<bool>());

            // Dates written as strings in configured format.
            Assert.AreEqual(JTokenType.String, row["DateCol"].Type);
            Assert.AreEqual("2024-06-15", row["DateCol"].Value<string>());
            Assert.AreEqual(JTokenType.String, row["DateTimeCol"].Type);
            Assert.AreEqual("2024-06-15T13:45:00.123", row["DateTimeCol"].Value<string>());

            // GUID written as string.
            Assert.AreEqual(JTokenType.String, row["GuidCol"].Type);
            Assert.AreEqual(guid.ToString(), row["GuidCol"].Value<string>());
        }
        finally
        {
            Helper.ExecuteNonQuery(_connString, $"DROP TABLE {table}");
        }
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_NullValues_WrittenAsJsonNull()
    {
        var input = new Input
        {
            Query = $"SELECT Id, LastName, TestNull FROM {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

        var content = await File.ReadAllTextAsync(_destination);
        var row = JArray.Parse(content)[0];

        Assert.AreEqual(JTokenType.Null, row["TestNull"].Type);
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_HandleNullAsEmpty_NullStringWrittenAsEmptyString()
    {
        _options.JsonOptions.HandleNullAsEmpty = true;

        var input = new Input
        {
            Query = $"SELECT Id, TestNull FROM {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

        var content = await File.ReadAllTextAsync(_destination);
        var row = JArray.Parse(content)[0];

        // String null → empty string.
        Assert.AreEqual(JTokenType.String, row["TestNull"].Type);
        Assert.AreEqual(string.Empty, row["TestNull"].Value<string>());
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_CustomDateFormat_AppliedCorrectly()
    {
        _options.JsonOptions.DateFormat = "dd.MM.yyyy";
        _options.JsonOptions.DateTimeFormat = "dd.MM.yyyy HH:mm";

        var table = "DateFormatTest";
        var createSql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}')
                BEGIN
                    CREATE TABLE {table} (DateCol date, DateTimeCol datetime2);
                END";

        Helper.CreateTestTable(_connString, table, createSql);

        Helper.ExecuteNonQuery(_connString, $"INSERT INTO {table} VALUES ('2024-06-15', '2024-06-15T13:45:00')");

        var input = new Input
        {
            Query = $"SELECT * FROM {table}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        try
        {
            await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

            var content = await File.ReadAllTextAsync(_destination);
            var row = JArray.Parse(content)[0];

            Assert.AreEqual("15.06.2024", row["DateCol"].Value<string>());
            Assert.AreEqual("15.06.2024 13:45", row["DateTimeCol"].Value<string>());
        }
        finally
        {
            Helper.ExecuteNonQuery(_connString, $"DROP TABLE {table}");
        }
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_LargeRowCount_StreamsWithoutLoadingAllRowsIntoMemory()
    {
        const int RowCount = 100_000;
        const long MaxAllowedMemoryGrowthBytes = 200 * 1024 * 1024; // 200 MB

        var table = "LargeRowTest";
        var createSql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}')
                BEGIN
                    CREATE TABLE {table} (
                        Id          int,
                        Name        nvarchar(100),
                        Salary      decimal(18,2),
                        IsActive    bit,
                        CreatedAt   datetime2
                    );
                END";

        Helper.CreateTestTable(_connString, table, createSql);

        var insertSql = $@"
                WITH Numbers AS (SELECT TOP {RowCount} ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c)
                INSERT INTO {table}
                SELECT N, 'Name_' + CAST(N AS nvarchar), CAST(N AS decimal(18,2)) * 1.5, CAST(N % 2 AS bit), GETDATE()
                FROM Numbers";

        Helper.ExecuteNonQuery(_connString, insertSql);

        var input = new Input
        {
            Query = $"SELECT * FROM {table}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = Process.GetCurrentProcess().WorkingSet64;

            var result = await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryAfter = Process.GetCurrentProcess().WorkingSet64;

            Assert.AreEqual(RowCount, result.EntriesWritten);
            Assert.IsTrue(File.Exists(_destination));

            var memoryGrowth = memoryAfter - memoryBefore;
            Assert.Less(
                memoryGrowth,
                MaxAllowedMemoryGrowthBytes,
                $"Memory grew by {memoryGrowth / 1024 / 1024} MB, expected less than {MaxAllowedMemoryGrowthBytes / 1024 / 1024} MB");
        }
        finally
        {
            Helper.ExecuteNonQuery(_connString, $"DROP TABLE {table}");
        }
    }

    [Test]
    public async Task ExecuteQueryToFile_Json_LargeBinaryColumn_StreamsWithoutLoadingIntoMemory()
    {
        const int RowCount = 10;
        const int BinaryColumnSizeMb = 10;
        const long MaxAllowedMemoryGrowthBytes = 200 * 1024 * 1024; // 200 MB

        var table = "LargeBinaryTest";
        var createSql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}')
                BEGIN
                    CREATE TABLE {table} (Id int, Data varbinary(max));
                END";

        Helper.CreateTestTable(_connString, table, createSql);

        // Insert rows with ~10 MB binary each = ~100 MB total.
        for (int i = 1; i <= RowCount; i++)
        {
            var param = new Microsoft.Data.SqlClient.SqlParameter("@Data", SqlDbType.VarBinary)
            {
                Value = new byte[BinaryColumnSizeMb * 1024 * 1024],
            };
            Helper.ExecuteNonQuery(_connString, $"INSERT INTO {table} VALUES ({i}, @Data)", new[] { param });
        }

        var input = new Input
        {
            Query = $"SELECT * FROM {table}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = Process.GetCurrentProcess().WorkingSet64;

            var result = await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryAfter = Process.GetCurrentProcess().WorkingSet64;

            Assert.AreEqual(RowCount, result.EntriesWritten);
            Assert.IsTrue(File.Exists(_destination));

            var memoryGrowth = memoryAfter - memoryBefore;
            Assert.Less(
                memoryGrowth,
                MaxAllowedMemoryGrowthBytes,
                $"Memory grew by {memoryGrowth / 1024 / 1024} MB, expected less than {MaxAllowedMemoryGrowthBytes / 1024 / 1024} MB");
        }
        finally
        {
            Helper.ExecuteNonQuery(_connString, $"DROP TABLE {table}");
        }
    }

    [Test]
    [Ignore("Requires significant disk space and time. Run manually to verify multi-GB streaming.")]
    public async Task ExecuteQueryToFile_Json_MultiGigabyte_StreamsWithoutLoadingAllRowsIntoMemory()
    {
        const int RowCount = 5_000_000;
        const int BatchSize = 100_000;
        const long MaxAllowedMemoryGrowthBytes = 200 * 1024 * 1024; // 200 MB

        var createSql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='MultiGigabyteTest')
                BEGIN
                    CREATE TABLE MultiGigabyteTest (
                        Id          int,
                        Name        nvarchar(200),
                        Description nvarchar(500),
                        Salary      decimal(18,2),
                        IsActive    bit,
                        CreatedAt   datetime2
                    );
                END";

        var table = "MultiGigabyteTest";
        Helper.CreateTestTable(_connString, table, createSql);

        // Insert in batches to avoid timeout
        int inserted = 0;
        while (inserted < RowCount)
        {
            int batchEnd = Math.Min(inserted + BatchSize, RowCount);

            var insertSql = $@"
                WITH Numbers AS (
                    SELECT TOP {batchEnd - inserted} ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) + {inserted} AS N
                    FROM sys.objects a CROSS JOIN sys.objects b CROSS JOIN sys.objects c
                )
                INSERT INTO {table}
                SELECT 
                    N,
                    REPLICATE('Name_' + CAST(N AS nvarchar), 5),
                    REPLICATE('Description_' + CAST(N AS nvarchar), 10),
                    CAST(N AS decimal(18,2)) * 1.5,
                    CAST(N % 2 AS bit),
                    GETDATE()
                FROM Numbers";

            Helper.ExecuteNonQuery(_connString, insertSql);
            inserted = batchEnd;
        }

        var input = new Input
        {
            Query = $"SELECT * FROM {table}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = Process.GetCurrentProcess().WorkingSet64;

            var result = await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryAfter = Process.GetCurrentProcess().WorkingSet64;

            Assert.AreEqual(RowCount, result.EntriesWritten);
            Assert.IsTrue(File.Exists(_destination));

            var fileSizeGb = new FileInfo(_destination).Length / 1024.0 / 1024.0 / 1024.0;
            TestContext.WriteLine($"File size: {fileSizeGb:F2} GB");
            TestContext.WriteLine($"Memory before: {memoryBefore / 1024 / 1024} MB");
            TestContext.WriteLine($"Memory after: {memoryAfter / 1024 / 1024} MB");
            TestContext.WriteLine($"Memory growth: {(memoryAfter - memoryBefore) / 1024 / 1024} MB");

            var memoryGrowth = memoryAfter - memoryBefore;
            Assert.Less(
                memoryGrowth,
                MaxAllowedMemoryGrowthBytes,
                $"Memory grew by {memoryGrowth / 1024 / 1024} MB, expected less than {MaxAllowedMemoryGrowthBytes / 1024 / 1024} MB");
        }
        finally
        {
            Helper.ExecuteNonQuery(_connString, $"DROP TABLE {table}");
        }
    }
}
