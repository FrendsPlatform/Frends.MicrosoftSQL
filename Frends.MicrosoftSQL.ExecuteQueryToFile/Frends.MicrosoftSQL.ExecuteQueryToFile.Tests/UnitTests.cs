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
    private static readonly string _connString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!";
    private static readonly string _tableName = "TestTable";
    private static readonly string _destination = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestData/test.csv");

    [SetUp]
    public void Init()
    {
        using (var connection = new SqlConnection(_connString))
        {
            connection.Open();
            var createTable = connection.CreateCommand();
            createTable.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN CREATE TABLE {_tableName} ( Id int, LastName varchar(255), FirstName varchar(255), Salary decimal(6,2), Image Image, TestText VarBinary(MAX)); END";
            createTable.ExecuteNonQuery();
            connection.Close();
        }

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

        InsertTestData($"Insert into {_tableName} (Id, LastName, FirstName, Salary, Image, TestText) values (1,'Meikalainen','Matti',1523.25, {parameters[0].ParameterName}, {parameters[1].ParameterName});", parameters);
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

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_image.png"))), output.TrimEnd(Environment.NewLine.ToCharArray()));
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

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_Text.txt"))), output.TrimEnd(Environment.NewLine.ToCharArray()));
    }

    private static void InsertTestData(string commandText, System.Data.SqlClient.SqlParameter[] parameters = null)
    {
        using var sqlConnection = new SqlConnection(_connString);
        sqlConnection.Open();

        using (var command = new SqlCommand())
        {
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.Connection = sqlConnection;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
            }

            command.ExecuteNonQuery();
        }

        sqlConnection.Close();
    }
}