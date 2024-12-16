namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using Frends.MicrosoftSQL.ExecuteQueryToFile.Enums;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
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

    private Options _options;

    [SetUp]
    public void Init()
    {
        _options = new Options()
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
    public async Task ExecuteQueryToFile_SqlParameters()
    {
        var query = new Input
        {
            Query = $"Select Id, LastName, FirstName, REPLACE(Salary, '.', ',') AS 'Salary' from {_tableName} WHERE Id = @param",
            QueryParameters = new Definitions.SqlParameter[]
            {
                new Definitions.SqlParameter
                {
                    Name = "@param",
                    Value = 1,
                    SqlDataType = SqlDataTypes.Int,
                },
            },
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        _options.CsvOptions.AddQuotesToStrings = true;
        _options.CsvOptions.IncludeHeadersInOutput = true;

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);
        var output = File.ReadAllText(_destination);

        Assert.AreEqual("Id;LastName;FirstName;Salary\r\n1;\"Meikalainen\";\"Matti\";\"1523,25\"\r\n", output);
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

        _options.CsvOptions.AddQuotesToStrings = true;
        _options.CsvOptions.IncludeHeadersInOutput = true;

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);
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

        _options.CsvOptions.IncludeHeadersInOutput = true;

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);

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

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);

        var output = File.ReadAllText(_destination);

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_image.png"))), output.TrimEnd(new char[] { '\r', '\n' }));
    }

    [Test]
    public async Task ExecuteQueryToFile_WithImageDBTypeWithWhereClause()
    {
        var query = new Input
        {
            Query = $"SELECT Image from {_tableName} WHERE CONVERT(varbinary(max), Image) = @param",
            QueryParameters = new Definitions.SqlParameter[]
            {
                new Definitions.SqlParameter
                {
                    Name = "@param",
                    Value = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_image.png")),
                    SqlDataType = SqlDataTypes.VarBinary,
                },
            },
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);

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

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);

        var output = File.ReadAllText(_destination);

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_text.txt"))), output.TrimEnd(new char[] { '\r', '\n' }));
    }

    [Test]
    public async Task ExecuteQueryToFile_WithBinaryDBTypeWithWhereClause()
    {
        var query = new Input
        {
            Query = $"SELECT TestText FROM {_tableName} WHERE TestText = @param",
            QueryParameters = new Definitions.SqlParameter[]
            {
                new Definitions.SqlParameter
                {
                    Name = "@param",
                    Value = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_text.txt")),
                    SqlDataType = SqlDataTypes.VarBinary,
                },
            },
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);

        var output = File.ReadAllText(_destination);

        Assert.AreEqual(BitConverter.ToString(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_destination), "Test_text.txt"))), output.TrimEnd(new char[] { '\r', '\n' }));
    }

    [Test]
    public async Task ExecuteQueryToFile_WithNULLParameter()
    {
        var query = new Input
        {
            Query = $"SELECT * FROM {_tableName} WHERE TestNull = @param",
            QueryParameters = new Definitions.SqlParameter[]
            {
                new Definitions.SqlParameter
                {
                    Name = "@param",
                    Value = null,
                    SqlDataType = SqlDataTypes.VarChar,
                },
            },
            ConnectionString = _connString,
            OutputFilePath = _destination,
        };

        await MicrosoftSQL.ExecuteQueryToFile(query, _options, default);

        var output = File.ReadAllText(_destination);

        Assert.IsNotNull(output);
    }

    [Test]
    public async Task ExecuteQueryToFile_TestWithGeographyData()
    {
        var table = "geographytest";
        var query = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}') BEGIN CREATE TABLE {table} ( Id int IDENTITY(1, 1), GeogCol1 geography, GeogCol2 AS GeogCol1.STAsText()); END";

        Helper.CreateTestTable(_connString, table, query);

        Helper.ExecuteNonQuery(_connString, $"INSERT INTO {table} (GeogCol1) VALUES (geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656 )', 4326));");
        Helper.ExecuteNonQuery(_connString, $"INSERT INTO {table} (GeogCol1) VALUES(geography::STGeomFromText('POLYGON((-122.358 47.653 , -122.348 47.649, -122.348 47.658, -122.358 47.658, -122.358 47.653))', 4326));");

        var input = new Input
        {
            ConnectionString = _connString,
            Query = $"SELECT * From {table}",
            OutputFilePath = _destination,
        };

        try
        {
            input.Query = $"SELECT * From {table}";

            var select = await MicrosoftSQL.ExecuteQueryToFile(input, _options, default);

            var output = File.ReadAllLines(_destination);
            Assert.AreEqual(2, output.Length);
            Assert.IsTrue(output[0].Split(";")[1].StartsWith("LINESTRING"));
            Assert.IsTrue(output[1].Split(";")[1].StartsWith("POLYGON"));
        }
        finally
        {
            Helper.ExecuteNonQuery(_connString, $"DROP TABLE {table}");
        }
    }
}