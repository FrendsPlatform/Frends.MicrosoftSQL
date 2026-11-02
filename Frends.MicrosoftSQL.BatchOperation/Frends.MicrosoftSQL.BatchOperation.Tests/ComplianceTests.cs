using Frends.MicrosoftSQL.BatchOperation.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.MicrosoftSQL.BatchOperation.Tests;

[TestClass]
public class ComplianceTests
{
    [TestMethod]
    public void TaskTypeIsStatic()
    {
        Assert.IsTrue(typeof(MicrosoftSQL).IsAbstract);
        Assert.IsTrue(typeof(MicrosoftSQL).IsSealed);
    }

    [TestMethod]
    public void ResultAndOptionsContainRequiredProperties()
    {
        Assert.IsNotNull(typeof(Result).GetProperty(nameof(Result.Error)));
        Assert.IsNotNull(typeof(Options).GetProperty(nameof(Options.ErrorMessageOnFailure)));
    }
}
