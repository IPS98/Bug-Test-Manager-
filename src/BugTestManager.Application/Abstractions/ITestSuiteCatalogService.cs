using BugTestManager.Application.ReadModels;

namespace BugTestManager.Application.Abstractions;

public interface ITestSuiteCatalogService
{
    IReadOnlyList<TestSuiteCatalogItem> GetCatalog();
}
