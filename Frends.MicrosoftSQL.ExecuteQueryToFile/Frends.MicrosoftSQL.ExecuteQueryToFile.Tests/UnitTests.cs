namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;
using NUnit.Framework;

/// <summary>
/// docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
/// with Git bash add winpty to the start of
/// winpty docker exec -it sql1 "bash"
/// /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
/// Check rows before CleanUp:
/// SELECT* FROM TestTable
/// GO
/// Optional queries:
/// SELECT Name FROM sys.Databases;
/// GO
/// SELECT* FROM INFORMATION_SCHEMA.TABLES;
/// GO
/// </summary>
[TestFixture]
public class UnitTests
{
    private static readonly string _connString = Helper.GetConnectionString();
    private static readonly string _tableName = "TestTable";
    private static readonly string _destination = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestData/test.csv");

    [SetUp]
    public void Init()
    {
        Helper.CreateTestTable(_connString, _tableName);

        var parameters = new System.Data.SqlClient.SqlParameter[]
        {
            new System.Data.SqlClient.SqlParameter("@Hash", SqlDbType.VarBinary)
            {
                Value = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_image.png")),
            },
            new System.Data.SqlClient.SqlParameter("@TestText", SqlDbType.VarBinary)
            {
                Value = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_text.txt")),
            },
        };

        Helper.InsertTestData(_connString, $"Insert into {_tableName} (Id, LastName, FirstName, Salary, Image, TestText) values (1,'Meikalainen','Matti',1523.25, {parameters[0].ParameterName}, {parameters[1].ParameterName});", parameters);
    }

    [TearDown]
    public void CleanUp()
    {
        using (var connection = new SqlConnection(_connString))
        {
            connection.Open();
            var createTable = connection.CreateCommand();
            createTable.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN DROP TABLE IF EXISTS {_tableName}; END";
            createTable.ExecuteNonQuery();
            connection.Close();
        }

        // Clean and remove destination directory
        File.Delete(_destination);
    }

    [Test]
    public async Task ExecuteQueryToFile_StringWithApostrophe()
    {
        var query = new Input
        {
            Query = $"Select Id, LastName, FirstName, REPLACE(Salary, '.', ',') AS 'Salary' from {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        var options = new Options
        {
            TimeoutSeconds = 30,
            CsvOptions = new CsvOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                LineBreak = CsvLineBreak.CRLF,
                FileEncoding = FileEncoding.UTF8,
                EnableBom = false,
                IncludeHeadersInOutput = true,
                SanitizeColumnHeaders = false,
                AddQuotesToDates = false,
                AddQuotesToStrings = true,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss",
            },
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, options, default);
        var output = File.ReadAllText(_destination);

        Assert.AreEqual("Id;LastName;FirstName;Salary\r\n1;\"Meikalainen\";\"Matti\";\"1523,25\"\r\n", output);
    }

    [Test]
    public async Task ExecuteQueryToFile_StringWithoutApostrophe()
    {
        var query = new Input
        {
            Query = $"Select Id, LastName, FirstName, REPLACE(Salary, '.', ',') AS 'Salary' from {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        var options = new Options
        {
            TimeoutSeconds = 30,
            CsvOptions = new CsvOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                LineBreak = CsvLineBreak.CRLF,
                FileEncoding = FileEncoding.UTF8,
                EnableBom = false,
                IncludeHeadersInOutput = true,
                SanitizeColumnHeaders = false,
                AddQuotesToDates = false,
                AddQuotesToStrings = false,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss",
            },
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, options, default);

        var output = File.ReadAllText(_destination);

        Assert.AreEqual("Id;LastName;FirstName;Salary\r\n1;Meikalainen;Matti;1523,25\r\n", output);
    }

    [Test]
    public async Task ExecuteQueryToFile_WithImageDBType()
    {
        var query = new Input
        {
            Query = $"SELECT Image from {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        var options = new Options
        {
            TimeoutSeconds = 30,
            CsvOptions = new CsvOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                LineBreak = CsvLineBreak.CRLF,
                FileEncoding = FileEncoding.UTF8,
                EnableBom = false,
                IncludeHeadersInOutput = false,
                SanitizeColumnHeaders = false,
                AddQuotesToDates = false,
                AddQuotesToStrings = false,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss",
            },
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, options, default);

        var output = File.ReadAllText(_destination);

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_image.png"))), output.TrimEnd(new char[] { '\r', '\n' }));
    }

    [Test]
    public async Task ExecuteQueryToFile_WithBinaryDBType()
    {
        var query = new Input
        {
            Query = $"SELECT TestText from {_tableName}",
            QueryParameters = Array.Empty<Definitions.SqlParameter>(),
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        var options = new Options
        {
            TimeoutSeconds = 30,
            CsvOptions = new CsvOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                LineBreak = CsvLineBreak.CRLF,
                FileEncoding = FileEncoding.UTF8,
                EnableBom = false,
                IncludeHeadersInOutput = false,
                SanitizeColumnHeaders = false,
                AddQuotesToDates = false,
                AddQuotesToStrings = false,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss",
            },
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, options, default);

        var output = File.ReadAllText(_destination);

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_text.txt"))), output.TrimEnd(Environment.NewLine.ToCharArray()));
    }
}