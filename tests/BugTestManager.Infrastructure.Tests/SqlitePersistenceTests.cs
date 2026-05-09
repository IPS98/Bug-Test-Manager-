using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Requests;
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

    [Fact]
    public void ManagementService_CreatesTestSuiteWithRequiredRevision()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var result = serviceProvider
            .GetRequiredService<ITestSuiteManagementService>()
            .CreateTestSuite(new CreateTestSuiteRequest(
                "Firmware Team Validation",
                "Checks owned by the firmware team.",
                RevisionIsRequired: true,
                InitialRevisionName: "Revision A"));

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdSuite = catalog.Single(testSuite => testSuite.Id == result.TestSuiteId);

        Assert.True(createdSuite.RevisionIsRequired);
        Assert.Equal("Firmware Team Validation", createdSuite.Name);
        Assert.Equal(result.InitialRevisionId, createdSuite.Revisions.Single().Id);
        Assert.Equal("Revision A", createdSuite.Revisions.Single().Name);
    }

    [Fact]
    public void ManagementService_CreatesSectionForOptionalRevisionSuite()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Bench Checks",
            "Manual checks without formal revisions.",
            RevisionIsRequired: false,
            InitialRevisionName: null));

        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Front Panel",
            "Controls"));

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdSuite = catalog.Single(suite => suite.Id == testSuite.TestSuiteId);
        var noRevision = createdSuite.Revisions.Single();
        var createdSection = noRevision.Sections.Single(section => section.Id == sectionId);

        Assert.Equal("No revision", noRevision.Name);
        Assert.Equal("Front Panel", createdSection.Name);
        Assert.Equal("Controls", createdSection.Category);
    }

    [Fact]
    public void ManagementService_CreatesTestCaseAndStepInsideSection()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Release Smoke Test",
            "Small release acceptance set.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Main Window",
            "UI"));

        var testCaseId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Open main window",
            "Main window opens without errors."));
        var stepId = managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Click the application shortcut.",
            "The application shell is visible."));

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdSection = catalog
            .Single(suite => suite.Id == testSuite.TestSuiteId)
            .Revisions.Single()
            .Sections.Single(section => section.Id == sectionId);
        var createdTestCase = createdSection.TestCases.Single(testCase => testCase.Id == testCaseId);
        var createdStep = createdTestCase.Steps.Single(step => step.Id == stepId);

        Assert.Equal("Open main window", createdTestCase.Title);
        Assert.Equal("Main window opens without errors.", createdTestCase.ExpectedResult);
        Assert.Equal("Click the application shortcut.", createdStep.StepText);
        Assert.Equal("The application shell is visible.", createdStep.ExpectedResult);
    }

    [Fact]
    public void ManagementService_RequiresInitialRevisionNameWhenRevisionIsRequired()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();

        Assert.Throws<ArgumentException>(() => managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Revisioned Suite",
            "Missing initial revision name.",
            RevisionIsRequired: true,
            InitialRevisionName: "")));
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
