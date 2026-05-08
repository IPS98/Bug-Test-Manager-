using BugTestManager.Application.Abstractions;
using BugTestManager.Infrastructure;
using BugTestManager.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BugTestManager.Infrastructure.Tests;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public void Initialize_CreatesDatabaseAndSeedsCatalog()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);

        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();

        Assert.True(File.Exists(databasePath));
        Assert.NotEmpty(catalog);
        Assert.Contains(catalog, testSuite => testSuite.Name == "Power Module Acceptance");
    }

    [Fact]
    public void CatalogService_ReturnsSuiteWithOptionalRevision()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var optionalRevisionSuite = catalog.Single(testSuite => !testSuite.RevisionIsRequired);

        Assert.Equal("Application UI Regression", optionalRevisionSuite.Name);
        Assert.Single(optionalRevisionSuite.Revisions);
        Assert.Equal("No revision", optionalRevisionSuite.Revisions[0].Name);
        Assert.NotEmpty(optionalRevisionSuite.Revisions[0].Sections);
    }

    private static ServiceProvider CreateServiceProvider(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(databasePath);

        return services.BuildServiceProvider();
    }

    private static string CreateTempDatabasePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BugTestManager.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "test.db");
    }
}
