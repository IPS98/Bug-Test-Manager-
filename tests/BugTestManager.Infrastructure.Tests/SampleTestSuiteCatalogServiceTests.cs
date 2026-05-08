using BugTestManager.Infrastructure.SampleData;

namespace BugTestManager.Infrastructure.Tests;

public sealed class SampleTestSuiteCatalogServiceTests
{
    [Fact]
    public void GetCatalog_ReturnsBrowsableTemplateHierarchy()
    {
        var service = new SampleTestSuiteCatalogService();

        var catalog = service.GetCatalog();
        var firstSuite = catalog[0];
        var firstRevision = firstSuite.Revisions[0];
        var firstSection = firstRevision.Sections[0];
        var firstTestCase = firstSection.TestCases[0];

        Assert.NotEmpty(catalog);
        Assert.NotEmpty(firstSuite.Revisions);
        Assert.NotEmpty(firstRevision.Sections);
        Assert.NotEmpty(firstSection.TestCases);
        Assert.NotEmpty(firstTestCase.Steps);
    }
}
