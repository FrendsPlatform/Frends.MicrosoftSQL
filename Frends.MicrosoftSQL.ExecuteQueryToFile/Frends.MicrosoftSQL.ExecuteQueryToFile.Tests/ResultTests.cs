namespace Frends.MicrosoftSQL.ExecuteQueryToFile.Tests;

using Frends.MicrosoftSQL.ExecuteQueryToFile.Definitions;
using NUnit.Framework;

[TestFixture]
public class ResultTests
{
    [Test]
    public void SuccessfulResultIncludesSuccessAndNoError()
    {
        var result = new Result(1, "C:\\exports\\customers.csv", "customers.csv");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.EntriesWritten, Is.EqualTo(1));
    }
}
