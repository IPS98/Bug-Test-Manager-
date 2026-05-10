using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
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
    public void ManagementService_UpdatesTemplateHierarchyItems()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Editable Suite",
            "Original suite description.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Original Section",
            "Original Category"));
        var testCaseId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Original Case",
            "Original case expected result."));
        var stepId = managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Original step.",
            "Original step expected result."));

        managementService.UpdateTestSuite(new UpdateTestSuiteRequest(
            testSuite.TestSuiteId,
            "Updated Suite",
            "Updated suite description."));
        managementService.UpdateSection(new UpdateTemplateSectionRequest(
            sectionId,
            "Updated Section",
            "Updated Category"));
        managementService.UpdateTestCase(new UpdateTestCaseTemplateRequest(
            testCaseId,
            "Updated Case",
            "Updated case expected result."));
        managementService.UpdateTestStep(new UpdateTestStepTemplateRequest(
            stepId,
            "Updated step.",
            "Updated step expected result."));

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var updatedSuite = catalog.Single(suite => suite.Id == testSuite.TestSuiteId);
        var updatedSection = updatedSuite.Revisions.Single().Sections.Single(section => section.Id == sectionId);
        var updatedCase = updatedSection.TestCases.Single(testCase => testCase.Id == testCaseId);
        var updatedStep = updatedCase.Steps.Single(step => step.Id == stepId);

        Assert.Equal("Updated Suite", updatedSuite.Name);
        Assert.Equal("Updated suite description.", updatedSuite.Description);
        Assert.Equal("Updated Section", updatedSection.Name);
        Assert.Equal("Updated Category", updatedSection.Category);
        Assert.Equal("Updated Case", updatedCase.Title);
        Assert.Equal("Updated case expected result.", updatedCase.ExpectedResult);
        Assert.Equal("Updated step.", updatedStep.StepText);
        Assert.Equal("Updated step expected result.", updatedStep.ExpectedResult);
    }

    [Fact]
    public void ManagementService_DeletesSingleStep()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Step Delete Check",
            "Used to verify step deletion.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Controls",
            "UI"));
        var testCaseId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Change voltage",
            "Voltage changes."));
        var stepId = managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Enter a new voltage.",
            "The value is accepted."));

        managementService.DeleteTestStep(stepId);

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdTestCase = catalog
            .Single(suite => suite.Id == testSuite.TestSuiteId)
            .Revisions.Single()
            .Sections.Single(section => section.Id == sectionId)
            .TestCases.Single(testCase => testCase.Id == testCaseId);

        Assert.Empty(createdTestCase.Steps);
    }

    [Fact]
    public void ManagementService_DeletesSectionWithChildItems()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Section Delete Check",
            "Used to verify cascade deletion.",
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
            "Window opens."));
        managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Click shortcut.",
            "Application opens."));

        managementService.DeleteSection(sectionId);

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdSuite = catalog.Single(suite => suite.Id == testSuite.TestSuiteId);

        Assert.DoesNotContain(
            createdSuite.Revisions.Single().Sections,
            section => section.Id == sectionId);
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

    [Fact]
    public void FieldDefinitionService_CreatesAndReturnsDefinitions()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseTemplate,
            "Firmware",
            FieldType.Text,
            IsRequired: true,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));

        var fields = fieldService.GetDefinitions();
        var field = fields.Single(definition => definition.Id == fieldId);

        Assert.Equal(EntityReferenceType.TestCaseTemplate, field.TargetEntityType);
        Assert.Equal("Firmware", field.Name);
        Assert.Equal(FieldType.Text, field.FieldType);
        Assert.Equal(CustomFieldScopeType.Global, field.ScopeType);
        Assert.Null(field.ScopeEntityId);
        Assert.Equal("All matching items", field.ScopeDisplayName);
        Assert.True(field.IsRequired);
        Assert.True(field.IsActive);
    }

    [Fact]
    public void FieldDefinitionService_ArchivesDefinitions()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.BugReport,
            "Severity",
            FieldType.SingleSelect,
            IsRequired: true,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: ["Low", "Medium", "High"]));

        fieldService.ArchiveDefinition(fieldId);

        var field = fieldService.GetDefinitions().Single(definition => definition.Id == fieldId);

        Assert.False(field.IsActive);
        Assert.Equal(["Low", "Medium", "High"], field.Options);
    }

    [Fact]
    public void FieldDefinitionService_CreatesScopedDefinitions()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Scoped Field Suite",
            "Used to verify scoped fields.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();

        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Firmware",
            FieldType.Text,
            IsRequired: true,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.TestSuite,
            ScopeEntityId: testSuite.TestSuiteId,
            ScopeDisplayName: "Test suite: Scoped Field Suite",
            Options: []));

        var field = fieldService.GetDefinitions().Single(definition => definition.Id == fieldId);

        Assert.Equal(CustomFieldScopeType.TestSuite, field.ScopeType);
        Assert.Equal(testSuite.TestSuiteId, field.ScopeEntityId);
        Assert.Equal("Test suite: Scoped Field Suite", field.ScopeDisplayName);
    }

    [Fact]
    public void TestSessionService_CreatesSessionFromTemplate()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();

        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression for 1.2.3",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.3",
            BuildNumber: "456",
            Notes: "First copied session.",
            CreatedBy: "tester"));

        var session = sessionService.GetSession(sessionId);

        Assert.Equal("Regression for 1.2.3", session.Name);
        Assert.Equal("Application UI Regression", session.TestSuiteName);
        Assert.Equal("1.2.3", session.TestedVersion);
        Assert.NotEmpty(session.Sections);
        Assert.All(session.Sections.SelectMany(section => section.TestCases), testCase =>
            Assert.Equal(TestResultStatus.NotTested, testCase.Status));
        Assert.All(session.Sections.SelectMany(section => section.TestCases).SelectMany(testCase => testCase.Steps), step =>
            Assert.Equal(TestResultStatus.NotTested, step.Status));
    }

    [Fact]
    public void TestSessionService_UpdatesCaseAndCheckResults()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();

        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression with results",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.4",
            BuildNumber: "789",
            Notes: "",
            CreatedBy: "tester"));

        var session = sessionService.GetSession(sessionId);
        var testCase = session.Sections
            .SelectMany(section => section.TestCases)
            .First(item => item.Steps.Count > 0);
        var step = testCase.Steps.First();

        sessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
            testCase.Id,
            TestResultStatus.Pass,
            "The full case passed."));

        var passedSession = sessionService.GetSession(sessionId);
        var passedCase = passedSession.Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal(TestResultStatus.Pass, passedCase.Status);
        Assert.Equal("The full case passed.", passedCase.Comment);
        Assert.All(passedCase.Steps, item => Assert.Equal(TestResultStatus.Pass, item.Status));

        sessionService.UpdateTestStepResult(new UpdateTestStepResultRequest(
            step.Id,
            TestResultStatus.Fail,
            "Tooltip text is missing."));

        var updatedSession = sessionService.GetSession(sessionId);
        var updatedCase = updatedSession.Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);
        var updatedStep = updatedCase.Steps.Single(item => item.Id == step.Id);

        Assert.Equal(TestResultStatus.Fail, updatedCase.Status);
        Assert.Equal("The full case passed.", updatedCase.Comment);
        Assert.Equal(TestResultStatus.Fail, updatedStep.Status);
        Assert.Equal("Tooltip text is missing.", updatedStep.Comment);
    }

    [Fact]
    public void TestSessionService_RequiresRevisionForRevisionedSuite()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.RevisionIsRequired);
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();

        Assert.Throws<InvalidOperationException>(() => sessionService.CreateSession(new CreateTestSessionRequest(
            "Missing revision",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.3",
            BuildNumber: "456",
            Notes: "",
            CreatedBy: "tester")));
    }

    [Fact]
    public void AttachmentService_AddsAttachmentForTestCaseResult()
    {
        var databasePath = CreateTempDatabasePath();
        var attachmentRootPath = CreateTempDirectoryPath();
        using var serviceProvider = CreateServiceProvider(databasePath, attachmentRootPath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression with evidence",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.5",
            BuildNumber: "790",
            Notes: "",
            CreatedBy: "tester"));
        var testCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .First();

        var sourceDirectory = CreateTempDirectoryPath();
        var sourceFilePath = Path.Combine(sourceDirectory, "evidence.txt");
        File.WriteAllText(sourceFilePath, "Screenshot placeholder.");

        var attachmentService = serviceProvider.GetRequiredService<IAttachmentService>();
        var attachmentId = attachmentService.AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            sourceFilePath,
            "tester"));

        var attachment = attachmentService
            .GetAttachments(EntityReferenceType.TestCaseResult, testCase.Id)
            .Single(item => item.Id == attachmentId);

        Assert.Equal("evidence.txt", attachment.OriginalFileName);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal("tester", attachment.UploadedBy);
        Assert.True(File.Exists(Path.Combine(attachmentRootPath, attachment.RelativePath)));
    }

    private static ServiceProvider CreateServiceProvider(string databasePath, string? attachmentRootPath = null)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(databasePath, attachmentRootPath);

        return services.BuildServiceProvider();
    }

    private static string CreateTempDatabasePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BugTestManager.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "test.db");
    }

    private static string CreateTempDirectoryPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BugTestManager.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        return directory;
    }
}
