using BugTestManager.Domain.Entities;

namespace BugTestManager.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void AddVersion_StoresVersionWithParentProduct()
    {
        var product = new Product("Power Supply Manager");

        var version = product.AddVersion("2.5.0", new DateOnly(2026, 5, 8));

        Assert.Equal(product.Id, version.ProductId);
        Assert.Equal("2.5.0", version.VersionName);
        Assert.Single(product.Versions);
    }

    [Fact]
    public void AddVersion_DoesNotAllowDuplicateVersionNames()
    {
        var product = new Product("Power Supply Manager");
        product.AddVersion("2.5.0");

        Assert.Throws<InvalidOperationException>(() => product.AddVersion("2.5.0"));
    }

    [Fact]
    public void AddTestSuite_StoresTestSuiteWithParentProduct()
    {
        var product = new Product("Power Supply Manager");

        var testSuite = product.AddTestSuite("Electrical checks");

        Assert.Equal(product.Id, testSuite.ProductId);
        Assert.Equal("Electrical checks", testSuite.Name);
        Assert.Single(product.TestSuites);
    }
}
