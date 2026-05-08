using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;

namespace BugTestManager.Infrastructure.SampleData;

public sealed class SampleTestSuiteCatalogService : ITestSuiteCatalogService
{
    public IReadOnlyList<TestSuiteCatalogItem> GetCatalog()
    {
        return SampleTestSuiteCatalogSource.GetCatalog();
    }
}
