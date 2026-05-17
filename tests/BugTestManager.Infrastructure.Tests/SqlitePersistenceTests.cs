using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Exceptions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure;
using BugTestManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
            "Updated suite description.",
            RevisionIsRequired: false));
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
    public void FieldDefinitionService_UpdatesDefinitionsAndScopes()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Updated Scope Suite",
            "Used to verify field re-scoping.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.BugReport,
            "Module",
            FieldType.Text,
            IsRequired: false,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));

        fieldService.UpdateDefinition(new UpdateCustomFieldDefinitionRequest(
            fieldId,
            EntityReferenceType.TestCaseResult,
            "Power model",
            FieldType.SingleSelect,
            IsRequired: true,
            SortOrder: 2,
            ScopeType: CustomFieldScopeType.TestSuite,
            ScopeEntityId: testSuite.TestSuiteId,
            ScopeDisplayName: "Test suite: Updated Scope Suite",
            Options: ["PM-42", "PM-84"]));

        var field = fieldService.GetDefinitions().Single(definition => definition.Id == fieldId);

        Assert.Equal(EntityReferenceType.TestCaseResult, field.TargetEntityType);
        Assert.Equal("Power model", field.Name);
        Assert.Equal(FieldType.SingleSelect, field.FieldType);
        Assert.True(field.IsRequired);
        Assert.Equal(2, field.SortOrder);
        Assert.Equal(CustomFieldScopeType.TestSuite, field.ScopeType);
        Assert.Equal(testSuite.TestSuiteId, field.ScopeEntityId);
        Assert.Equal(["PM-42", "PM-84"], field.Options);
    }

    [Fact]
    public void CustomFieldValueService_SavesAndUpdatesBugValues()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.BugReport,
            "Affected module",
            FieldType.Text,
            IsRequired: false,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));
        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Power button tooltip is wrong",
            "The tooltip shows an old label.",
            "Medium",
            "Medium",
            "1.2.7",
            "801",
            "tester"));

        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();
        valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.BugReport,
            bugId,
            "Main Window",
            "tester"));
        valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.BugReport,
            bugId,
            "Power Controls",
            "developer"));

        var fieldValue = valueService
            .GetValues(EntityReferenceType.BugReport, bugId, [])
            .Single(item => item.FieldDefinitionId == fieldId);

        Assert.Equal("Affected module", fieldValue.Name);
        Assert.Equal("Power Controls", fieldValue.Value);
    }

    [Fact]
    public void FieldDefinitionService_DeletesDefinitionsAndValues()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.BugReport,
            "Affected module",
            FieldType.Text,
            IsRequired: false,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));
        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Graph refresh fails",
            "The graph stops updating.",
            "Medium",
            "High",
            "1.2.8",
            "802",
            "tester"));

        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();
        valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.BugReport,
            bugId,
            "Graphs",
            "tester"));

        fieldService.DeleteDefinition(fieldId);

        Assert.DoesNotContain(fieldService.GetDefinitions(), field => field.Id == fieldId);
        Assert.DoesNotContain(
            valueService.GetValues(EntityReferenceType.BugReport, bugId, []),
            field => field.FieldDefinitionId == fieldId);
    }

    [Fact]
    public void CustomFieldValueService_ReturnsDefinitionsForNewBug()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.BugReport,
            "Affected module",
            FieldType.Text,
            IsRequired: true,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));

        var values = serviceProvider
            .GetRequiredService<ICustomFieldValueService>()
            .GetValues(EntityReferenceType.BugReport, Guid.Empty, []);

        var field = values.Single(item => item.FieldDefinitionId == fieldId);
        Assert.True(field.IsRequired);
        Assert.Equal(string.Empty, field.Value);
    }

    [Fact]
    public void CustomFieldValueService_RejectsMissingRequiredValue()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.BugReport,
            "Affected module",
            FieldType.Text,
            IsRequired: true,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));

        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();

        Assert.Throws<InvalidOperationException>(() => valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.BugReport,
            Guid.NewGuid(),
            "",
            "tester")));
    }

    [Fact]
    public void CustomFieldValueService_ReturnsScopedTestResultValues()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Scoped Result Suite",
            "Used to verify scoped result fields.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Power Controls",
            "UI"));
        var testCaseTemplateId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "ON/OFF button",
            "Button toggles output."));
        managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseTemplateId,
            "Check tooltip.",
            "Tooltip is visible."));

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var globalFieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Firmware",
            FieldType.Text,
            IsRequired: false,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "All matching items",
            Options: []));
        var scopedFieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Power model",
            FieldType.Text,
            IsRequired: false,
            SortOrder: 2,
            ScopeType: CustomFieldScopeType.TestCaseTemplate,
            ScopeEntityId: testCaseTemplateId,
            ScopeDisplayName: "Test case: ON/OFF button",
            Options: []));

        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Scoped custom field run",
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            TestedVersion: "2.0.0",
            BuildNumber: "1001",
            Notes: "",
            CreatedBy: "tester"));
        var testCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .Single();

        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();
        valueService.SaveValue(new SaveCustomFieldValueRequest(
            scopedFieldId,
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            "PM-42",
            "tester"));

        var values = valueService.GetValues(
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            [new CustomFieldValueScopeItem(CustomFieldScopeType.TestCaseTemplate, testCaseTemplateId)]);

        Assert.Contains(values, item => item.FieldDefinitionId == globalFieldId);
        Assert.Contains(values, item => item.FieldDefinitionId == scopedFieldId && item.Value == "PM-42");
    }

    [Fact]
    public void CustomFieldValueService_ReturnsRequiredFieldAddedAfterSessionStart()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Late Field Suite",
            "Used to verify fields added after a session starts.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Power Controls",
            "UI"));
        var testCaseTemplateId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "ON/OFF button",
            "Button toggles output."));

        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Late field run",
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            TestedVersion: "2.1.0",
            BuildNumber: "1100",
            Notes: "",
            CreatedBy: "tester"));
        var testCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .Single();

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Firmware date",
            FieldType.Date,
            IsRequired: true,
            SortOrder: 1,
            ScopeType: CustomFieldScopeType.TestCaseTemplate,
            ScopeEntityId: testCaseTemplateId,
            ScopeDisplayName: "Test case: ON/OFF button",
            Options: []));

        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();
        var values = valueService.GetValues(
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            [new CustomFieldValueScopeItem(CustomFieldScopeType.TestCaseTemplate, testCaseTemplateId)]);
        var field = values.Single(value => value.FieldDefinitionId == fieldId);

        Assert.True(field.IsRequired);
        Assert.Equal(string.Empty, field.Value);

        var error = Assert.Throws<InvalidOperationException>(() => valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            "",
            "tester")));
        Assert.Contains("Firmware date", error.Message);

        valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            "2026-05-16",
            "tester"));

        var savedField = valueService
            .GetValues(
                EntityReferenceType.TestCaseResult,
                testCase.Id,
                [new CustomFieldValueScopeItem(CustomFieldScopeType.TestCaseTemplate, testCaseTemplateId)])
            .Single(value => value.FieldDefinitionId == fieldId);
        Assert.Equal("2026-05-16", savedField.Value);
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
    public void TestSessionService_CreatesManualSessionItemsWithoutTemplate()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateManualSession(new CreateManualTestSessionRequest(
            "Manual smoke test",
            "2.0.0",
            "1001",
            "Created without a template.",
            "tester"));
        var sectionId = sessionService.CreateManualSection(new CreateManualTestSectionRequest(
            sessionId,
            "Power Controls",
            "UI"));
        var testCaseId = sessionService.CreateManualTestCase(new CreateManualTestCaseRequest(
            sectionId,
            "ON/OFF button",
            "Button toggles the output state."));
        var checkId = sessionService.CreateManualCheck(new CreateManualTestCheckRequest(
            testCaseId,
            "Tooltip is visible.",
            "Tooltip explains the control."));

        var session = sessionService.GetSession(sessionId);
        var section = session.Sections.Single(item => item.Id == sectionId);
        var testCase = section.TestCases.Single(item => item.Id == testCaseId);
        var check = testCase.Steps.Single(item => item.Id == checkId);

        Assert.Equal("Manual Session", session.TestSuiteName);
        Assert.Null(session.TestSuiteRevisionName);
        Assert.Equal("Power Controls", section.Name);
        Assert.Equal("UI", section.Category);
        Assert.Equal("ON/OFF button", testCase.Title);
        Assert.Equal(TestResultStatus.NotTested, testCase.Status);
        Assert.Equal("Tooltip is visible.", check.StepText);
        Assert.Equal(TestResultStatus.NotTested, check.Status);
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

        Assert.Null(testCase.LastStatusChangedAt);
        Assert.Null(step.LastStatusChangedAt);

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
        Assert.NotNull(passedCase.LastStatusChangedAt);
        Assert.All(passedCase.Steps, item => Assert.Equal(TestResultStatus.Pass, item.Status));
        Assert.All(passedCase.Steps, item => Assert.NotNull(item.LastStatusChangedAt));

        var oldStatusChangedAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<BugTestManagerDbContext>>();
        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            var storedCase = dbContext.TestCaseResults
                .Include(item => item.Steps)
                .Single(item => item.Id == testCase.Id);
            storedCase.LastStatusChangedAt = oldStatusChangedAt;

            foreach (var storedStep in storedCase.Steps)
            {
                storedStep.LastStatusChangedAt = oldStatusChangedAt;
            }

            dbContext.SaveChanges();
        }

        var passedCaseStatusChangedAt = oldStatusChangedAt;
        var passedStepStatusChangedAt = oldStatusChangedAt;
        sessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
            testCase.Id,
            TestResultStatus.Pass,
            "Only the comment changed."));

        var commentOnlySession = sessionService.GetSession(sessionId);
        var commentOnlyCase = commentOnlySession.Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal("Only the comment changed.", commentOnlyCase.Comment);
        Assert.Equal(passedCaseStatusChangedAt, commentOnlyCase.LastStatusChangedAt);
        Assert.Equal(passedStepStatusChangedAt, commentOnlyCase.Steps.First().LastStatusChangedAt);

        sessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
            testCase.Id,
            TestResultStatus.Fail,
            "The full case failed."));

        var failedSession = sessionService.GetSession(sessionId);
        var failedCase = failedSession.Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal(TestResultStatus.Fail, failedCase.Status);
        Assert.Equal("The full case failed.", failedCase.Comment);
        Assert.All(failedCase.Steps, item => Assert.Equal(TestResultStatus.Fail, item.Status));
        Assert.All(failedCase.Steps, item => Assert.NotNull(item.LastStatusChangedAt));
        Assert.NotEqual(passedCaseStatusChangedAt, failedCase.LastStatusChangedAt);
        Assert.NotEqual(passedStepStatusChangedAt, failedCase.Steps.First().LastStatusChangedAt);

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
        Assert.Equal("The full case failed.", updatedCase.Comment);
        Assert.NotNull(updatedCase.LastStatusChangedAt);
        Assert.Equal(TestResultStatus.Fail, updatedStep.Status);
        Assert.Equal("Tooltip text is missing.", updatedStep.Comment);
        Assert.NotNull(updatedStep.LastStatusChangedAt);
    }

    [Fact]
    public void TestSessionService_BackfillsMissingStatusChangeDatesForExistingPassedChecks()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();

        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression with old result dates",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.5",
            BuildNumber: "790",
            Notes: "",
            CreatedBy: "tester"));

        var testCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .First(item => item.Steps.Count > 0);

        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<BugTestManagerDbContext>>();
        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            var oldCase = dbContext.TestCaseResults
                .Include(item => item.Steps)
                .Single(item => item.Id == testCase.Id);
            oldCase.Status = TestResultStatus.NotTested;
            oldCase.LastStatusChangedAt = null;

            foreach (var step in oldCase.Steps)
            {
                step.Status = TestResultStatus.Pass;
                step.LastStatusChangedAt = null;
            }

            dbContext.SaveChanges();
        }

        sessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
            testCase.Id,
            TestResultStatus.Pass,
            "Case passed after older check statuses existed."));

        var updatedCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal(TestResultStatus.Pass, updatedCase.Status);
        Assert.NotNull(updatedCase.LastStatusChangedAt);
        Assert.All(updatedCase.Steps, step => Assert.Equal(TestResultStatus.Pass, step.Status));
        Assert.All(updatedCase.Steps, step => Assert.NotNull(step.LastStatusChangedAt));
    }

    [Fact]
    public void TestSessionService_CalculatesCaseStatusFromChecks()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();

        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression with calculated case status",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.6",
            BuildNumber: "791",
            Notes: "",
            CreatedBy: "tester"));

        var testCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .First(item => item.Steps.Count > 1);

        foreach (var step in testCase.Steps)
        {
            sessionService.UpdateTestStepResult(new UpdateTestStepResultRequest(
                step.Id,
                TestResultStatus.Pass,
                $"Check {step.SortOrder} passed."));
        }

        var passedCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal(TestResultStatus.Pass, passedCase.Status);
        Assert.NotNull(passedCase.LastStatusChangedAt);

        var failedStep = passedCase.Steps.First();
        sessionService.UpdateTestStepResult(new UpdateTestStepResultRequest(
            failedStep.Id,
            TestResultStatus.Fail,
            "One check failed."));

        var failedCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal(TestResultStatus.Fail, failedCase.Status);
        Assert.Equal(TestResultStatus.Fail, failedCase.Steps.Single(step => step.Id == failedStep.Id).Status);
    }

    [Fact]
    public void TestSessionService_LoadsLinkedBugsForCaseAndCheckResults()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression with linked bugs",
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
        var check = testCase.Steps.First();

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var caseBugId = bugService.CreateBug(new CreateBugReportRequest(
            "Case level linked bug",
            "The full test case needs developer review.",
            "Medium",
            "Medium",
            "1.2.4",
            "789",
            "tester",
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            $"Test case: {testCase.Title}"));
        var checkBugId = bugService.CreateBug(new CreateBugReportRequest(
            "Check level linked bug",
            "The individual check needs developer review.",
            "High",
            "High",
            "1.2.4",
            "789",
            "tester",
            EntityReferenceType.TestStepResult,
            check.Id,
            $"Check {check.SortOrder}: {check.StepText}"));

        var refreshedSession = sessionService.GetSession(sessionId);
        var refreshedCase = refreshedSession.Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);
        var refreshedCheck = refreshedCase.Steps.Single(item => item.Id == check.Id);

        Assert.Contains(refreshedCase.LinkedBugs, bug => bug.Id == caseBugId && bug.Title == "Case level linked bug");
        Assert.Contains(refreshedCheck.LinkedBugs, bug => bug.Id == checkBugId && bug.Title == "Check level linked bug");
    }

    [Fact]
    public void TestSessionService_CopiesPreviousSessionAsCleanNewRun()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();

        var sourceSessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression 1.2.4",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.2.4",
            BuildNumber: "789",
            Notes: "Original run",
            CreatedBy: "tester"));

        var sourceSession = sessionService.GetSession(sourceSessionId);
        var sourceCase = sourceSession.Sections
            .SelectMany(section => section.TestCases)
            .First(item => item.Steps.Count > 0);
        var sourceCheck = sourceCase.Steps.First();

        sessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
            sourceCase.Id,
            TestResultStatus.Pass,
            "Original case comment."));
        sessionService.UpdateTestStepResult(new UpdateTestStepResultRequest(
            sourceCheck.Id,
            TestResultStatus.Fail,
            "Original check comment."));

        var copiedSessionId = sessionService.CopySession(new CopyTestSessionRequest(
            "Regression 1.2.5",
            sourceSessionId,
            TestedVersion: "1.2.5",
            BuildNumber: "790",
            Notes: "Copied structure for next build",
            CreatedBy: "tester"));

        var copiedSession = sessionService.GetSession(copiedSessionId);
        var copiedCase = copiedSession.Sections
            .SelectMany(section => section.TestCases)
            .First(item => item.Title == sourceCase.Title);
        var copiedCheck = copiedCase.Steps.First(item => item.StepText == sourceCheck.StepText);

        Assert.NotEqual(sourceSessionId, copiedSessionId);
        Assert.Equal("Regression 1.2.5", copiedSession.Name);
        Assert.Equal("1.2.5", copiedSession.TestedVersion);
        Assert.Equal("790", copiedSession.BuildNumber);
        Assert.Equal(sourceSession.Sections.Count, copiedSession.Sections.Count);
        Assert.Equal(sourceSession.Sections.Sum(section => section.TestCases.Count), copiedSession.Sections.Sum(section => section.TestCases.Count));
        Assert.Equal(sourceSession.Sections.Sum(section => section.TestCases.Sum(testCase => testCase.Steps.Count)), copiedSession.Sections.Sum(section => section.TestCases.Sum(testCase => testCase.Steps.Count)));
        Assert.NotEqual(sourceCase.Id, copiedCase.Id);
        Assert.Equal(sourceCase.TestCaseTemplateId, copiedCase.TestCaseTemplateId);
        Assert.Equal(TestResultStatus.NotTested, copiedCase.Status);
        Assert.Equal(string.Empty, copiedCase.Comment);
        Assert.NotEqual(sourceCheck.Id, copiedCheck.Id);
        Assert.Equal(sourceCheck.TestStepTemplateId, copiedCheck.TestStepTemplateId);
        Assert.Equal(TestResultStatus.NotTested, copiedCheck.Status);
        Assert.Equal(string.Empty, copiedCheck.Comment);
    }

    [Fact]
    public void TemplateSyncService_UpdatesOriginalTemplateWithManualSessionItems()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Template Sync Source",
            "Original reusable template.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Main Window",
            "UI"));
        var testCaseTemplateId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Navigation panel",
            "Navigation panel is visible."));
        managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseTemplateId,
            "Open the application.",
            "Main window opens."));

        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Template sync run",
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            TestedVersion: "2.0.0",
            BuildNumber: "1001",
            Notes: "",
            CreatedBy: "tester"));
        var session = sessionService.GetSession(sessionId);
        var existingCase = session.Sections.Single().TestCases.Single();

        var manualCheckId = sessionService.CreateManualCheck(new CreateManualTestCheckRequest(
            existingCase.Id,
            "Tooltip is visible.",
            "Tooltip explains the navigation action."));
        var manualSectionId = sessionService.CreateManualSection(new CreateManualTestSectionRequest(
            sessionId,
            "Reports",
            "Export"));
        var manualCaseId = sessionService.CreateManualTestCase(new CreateManualTestCaseRequest(
            manualSectionId,
            "PDF report",
            "PDF report is created."));
        sessionService.CreateManualCheck(new CreateManualTestCheckRequest(
            manualCaseId,
            "Export report.",
            "PDF file exists."));

        var syncService = serviceProvider.GetRequiredService<ITestSessionTemplateSyncService>();
        var preview = syncService.GetPreview(sessionId);

        Assert.True(preview.CanUpdateOriginalTemplate);
        Assert.Equal(1, preview.NewSectionCount);
        Assert.Equal(1, preview.NewTestCaseCount);
        Assert.Equal(2, preview.NewCheckCount);

        var result = syncService.UpdateOriginalTemplate(new UpdateTemplateFromSessionRequest(sessionId));
        var updatedSuite = serviceProvider
            .GetRequiredService<ITestSuiteCatalogService>()
            .GetCatalog()
            .Single(suite => suite.Id == testSuite.TestSuiteId);
        var allCases = updatedSuite.Revisions.Single().Sections.SelectMany(section => section.TestCases).ToList();
        var refreshedSession = sessionService.GetSession(sessionId);
        var refreshedManualCheck = refreshedSession.Sections
            .SelectMany(section => section.TestCases)
            .SelectMany(testCase => testCase.Steps)
            .Single(check => check.Id == manualCheckId);

        Assert.Equal(1, result.AddedSections);
        Assert.Equal(1, result.AddedTestCases);
        Assert.Equal(2, result.AddedChecks);
        Assert.Contains(updatedSuite.Revisions.Single().Sections, section => section.Name == "Reports");
        Assert.Contains(allCases, testCase => testCase.Title == "PDF report");
        Assert.NotEqual(Guid.Empty, refreshedManualCheck.TestStepTemplateId);
    }

    [Fact]
    public void TemplateSyncService_CreatesNewTemplateFromManualSession()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateManualSession(new CreateManualTestSessionRequest(
            "Manual firmware smoke",
            "2.0.0",
            "1001",
            "Manual structure should become a template.",
            "tester"));
        var sectionId = sessionService.CreateManualSection(new CreateManualTestSectionRequest(
            sessionId,
            "Power Controls",
            "UI"));
        var testCaseId = sessionService.CreateManualTestCase(new CreateManualTestCaseRequest(
            sectionId,
            "ON/OFF button",
            "Button toggles output."));
        sessionService.CreateManualCheck(new CreateManualTestCheckRequest(
            testCaseId,
            "Check tooltip.",
            "Tooltip is visible."));

        var syncService = serviceProvider.GetRequiredService<ITestSessionTemplateSyncService>();
        var preview = syncService.GetPreview(sessionId);

        Assert.False(preview.CanUpdateOriginalTemplate);

        var result = syncService.CreateTemplateFromSession(new CreateTemplateFromSessionRequest(
            sessionId,
            "Manual Firmware Smoke Template",
            "Created from a manual session."));
        var createdSuite = serviceProvider
            .GetRequiredService<ITestSuiteCatalogService>()
            .GetCatalog()
            .Single(suite => suite.Id == result.TestSuiteId);

        Assert.False(createdSuite.RevisionIsRequired);
        Assert.Equal("Manual Firmware Smoke Template", createdSuite.Name);
        Assert.Contains(createdSuite.Revisions.Single().Sections, section => section.Name == "Power Controls");
        Assert.Contains(
            createdSuite.Revisions.Single().Sections.SelectMany(section => section.TestCases),
            testCase => testCase.Title == "ON/OFF button");
        Assert.Equal(1, result.AddedSections);
        Assert.Equal(1, result.AddedTestCases);
        Assert.Equal(1, result.AddedChecks);
    }

    [Fact]
    public void ReportDataService_BuildsTestSessionReportData()
    {
        var databasePath = CreateTempDatabasePath();
        var attachmentRootPath = CreateTempDirectoryPath();
        using var serviceProvider = CreateServiceProvider(databasePath, attachmentRootPath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Regression report source",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.3.0",
            BuildNumber: "900",
            Notes: "Report notes",
            CreatedBy: "tester"));

        var session = sessionService.GetSession(sessionId);
        var testCase = session.Sections.SelectMany(section => section.TestCases).First();
        sessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
            testCase.Id,
            TestResultStatus.Pass,
            "Case passed for report."));

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Firmware",
            FieldType.Text,
            IsRequired: false,
            SortOrder: 1,
            CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "Whole project",
            Options: []));
        var dateFieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Review date",
            FieldType.Date,
            IsRequired: false,
            SortOrder: 2,
            CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "Whole project",
            Options: []));
        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();
        valueService.SaveValue(new SaveCustomFieldValueRequest(
            fieldId,
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            "FW-1.0.0",
            "tester"));
        valueService.SaveValue(new SaveCustomFieldValueRequest(
            dateFieldId,
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            "2026-05-16T11:45:00+00:00",
            "tester"));

        var sourceDirectory = CreateTempDirectoryPath();
        var sourceFilePath = Path.Combine(sourceDirectory, "report-evidence.txt");
        File.WriteAllText(sourceFilePath, "Evidence for report.");
        var attachmentService = serviceProvider.GetRequiredService<IAttachmentService>();
        attachmentService.AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            sourceFilePath,
            "tester"));

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Report linked bug",
            "Bug visible in report data.",
            "High",
            "P1",
            "1.3.0",
            "900",
            "tester",
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            $"Test case: {testCase.Title}"));

        var report = serviceProvider.GetRequiredService<IReportDataService>().GetTestSessionReport(sessionId);
        var reportCase = report.Sections
            .SelectMany(section => section.TestCases)
            .Single(item => item.Id == testCase.Id);

        Assert.Equal("Regression report source", report.SessionName);
        Assert.Equal("1.3.0", report.TestedVersion);
        Assert.Equal("900", report.BuildNumber);
        Assert.Equal(session.Sections.Sum(section => section.TestCases.Count), report.Summary.Total);
        Assert.Equal(1, report.Summary.Passed);
        Assert.Equal("Case passed for report.", reportCase.Comment);
        Assert.NotNull(reportCase.LastStatusChangedAt);
        Assert.Equal(10, reportCase.LastStatusChangedDateDisplay.Length);
        Assert.Contains(reportCase.CustomFields, field => field.Name == "Firmware" && field.Value == "FW-1.0.0");
        Assert.Contains(reportCase.CustomFields, field => field.Name == "Review date" && field.DisplayValue == "2026-05-16");
        Assert.Contains(reportCase.Attachments, attachment => attachment.OriginalFileName == "report-evidence.txt");
        Assert.Contains(report.LinkedBugs, bug => bug.Id == bugId && bug.Title == "Report linked bug");
    }

    [Fact]
    public void ReportExportService_ExportsTestSessionPdf()
    {
        var databasePath = CreateTempDatabasePath();
        var attachmentRootPath = CreateTempDirectoryPath();
        using var serviceProvider = CreateServiceProvider(databasePath, attachmentRootPath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var sourceSuite = catalog.Single(suite => suite.Name == "Application UI Regression");
        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "PDF report source",
            sourceSuite.Id,
            TestSuiteRevisionId: null,
            TestedVersion: "1.3.1",
            BuildNumber: "901",
            Notes: "PDF export notes",
            CreatedBy: "tester"));
        var testCase = sessionService.GetSession(sessionId)
            .Sections
            .SelectMany(section => section.TestCases)
            .First();
        var check = testCase.Steps.First();
        var sourceDirectory = CreateTempDirectoryPath();
        var sourceImagePath = Path.Combine(sourceDirectory, "evidence.png");
        File.WriteAllBytes(
            sourceImagePath,
            Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="));
        serviceProvider.GetRequiredService<IAttachmentService>().AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.TestCaseResult,
            testCase.Id,
            sourceImagePath,
            "tester"));
        var checkImagePath = Path.Combine(sourceDirectory, "check-evidence.png");
        File.Copy(sourceImagePath, checkImagePath);
        serviceProvider.GetRequiredService<IAttachmentService>().AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.TestStepResult,
            check.Id,
            checkImagePath,
            "tester"));

        var report = serviceProvider
            .GetRequiredService<IReportDataService>()
            .GetTestSessionReport(sessionId);
        var outputFilePath = Path.Combine(CreateTempDirectoryPath(), "test-session-report.pdf");

        var result = serviceProvider
            .GetRequiredService<IReportExportService>()
            .ExportTestSessionReport(new ExportTestSessionReportRequest(
                report,
                outputFilePath,
                "tester"));

        Assert.Equal(outputFilePath, result.OutputFilePath);
        Assert.True(File.Exists(outputFilePath));
        Assert.True(new FileInfo(outputFilePath).Length > 0);
        Assert.Equal("%PDF"u8.ToArray(), File.ReadAllBytes(outputFilePath).Take(4).ToArray());
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
        Assert.Equal(Path.Combine(attachmentRootPath, attachment.RelativePath), attachment.AbsolutePath);
        Assert.True(File.Exists(Path.Combine(attachmentRootPath, attachment.RelativePath)));
    }

    [Fact]
    public void AttachmentService_DeletesAttachmentMetadataAndFile()
    {
        var databasePath = CreateTempDatabasePath();
        var attachmentRootPath = CreateTempDirectoryPath();
        using var serviceProvider = CreateServiceProvider(databasePath, attachmentRootPath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var sourceDirectory = CreateTempDirectoryPath();
        var sourceFilePath = Path.Combine(sourceDirectory, "delete-me.log");
        File.WriteAllText(sourceFilePath, "temporary evidence");

        var attachmentService = serviceProvider.GetRequiredService<IAttachmentService>();
        var entityId = Guid.NewGuid();
        var attachmentId = attachmentService.AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.TestStepResult,
            entityId,
            sourceFilePath,
            "tester"));
        var attachment = attachmentService
            .GetAttachments(EntityReferenceType.TestStepResult, entityId)
            .Single(item => item.Id == attachmentId);

        attachmentService.DeleteAttachment(attachmentId);

        Assert.Empty(attachmentService.GetAttachments(EntityReferenceType.TestStepResult, entityId));
        Assert.False(File.Exists(attachment.AbsolutePath));
    }

    [Fact]
    public void BugReportService_CreatesAndUpdatesBugStatus()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Tooltip is missing",
            "The On/Off button does not show the expected tooltip.",
            "High",
            "Medium",
            "1.2.5",
            "790",
            "tester",
            EntityReferenceType.TestStepResult,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Check 3: Tooltip"));

        bugService.UpdateStatus(new UpdateBugStatusRequest(
            bugId,
            BugStatus.Fixed,
            "developer"));

        var bug = bugService.GetBugs().Single(item => item.Id == bugId);

        Assert.Equal("Tooltip is missing", bug.Title);
        Assert.Equal(BugStatus.Fixed, bug.Status);
        Assert.Equal("High", bug.Severity);
        Assert.Equal("Medium", bug.Priority);
        Assert.Equal("tester", bug.CreatedBy);
        Assert.Equal("developer", bug.UpdatedBy);
        Assert.Equal(EntityReferenceType.TestStepResult, bug.LinkedEntityType);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), bug.LinkedEntityId);
        Assert.Equal("Check 3: Tooltip", bug.LinkedEntityDisplayName);
    }

    [Fact]
    public void BugReportService_RejectsDuplicateBugTitle()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        bugService.CreateBug(new CreateBugReportRequest(
            "Tooltip is missing",
            "The On/Off button does not show the expected tooltip.",
            "High",
            "Medium",
            "1.2.5",
            "790",
            "tester"));

        Assert.Throws<DuplicateBugTitleException>(() => bugService.CreateBug(new CreateBugReportRequest(
            " tooltip IS missing ",
            "Duplicate title should not be accepted.",
            "Low",
            "Low",
            "1.2.6",
            "791",
            "tester")));
    }

    [Fact]
    public void BugReportService_UpdatesBugDetails()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Output voltage is unstable",
            "Voltage graph jumps during startup.",
            "High",
            "High",
            "1.2.6",
            "792",
            "tester"));

        bugService.UpdateBug(new UpdateBugReportRequest(
            bugId,
            "Output voltage is unstable after startup",
            "Voltage graph jumps during startup and load change.",
            BugStatus.InProgress,
            "Medium",
            "High",
            "1.2.7",
            "800",
            "developer"));

        var bug = bugService.GetBugs().Single(item => item.Id == bugId);

        Assert.Equal("Output voltage is unstable after startup", bug.Title);
        Assert.Equal("Voltage graph jumps during startup and load change.", bug.Description);
        Assert.Equal(BugStatus.InProgress, bug.Status);
        Assert.Equal("developer", bug.UpdatedBy);
    }

    [Fact]
    public void DiscussionService_AddsUpdatesAndDeletesComments()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Output voltage is unstable",
            "Voltage graph jumps during startup.",
            "High",
            "High",
            "1.2.6",
            "792",
            "tester"));

        var discussionService = serviceProvider.GetRequiredService<IDiscussionService>();
        var commentId = discussionService.AddComment(new AddDiscussionCommentRequest(
            EntityReferenceType.BugReport,
            bugId,
            "Developer note: checking startup initialization.",
            "developer"));

        discussionService.UpdateComment(new UpdateDiscussionCommentRequest(
            commentId,
            "Developer note: startup initialization is under review.",
            "developer"));

        var comment = discussionService
            .GetComments(EntityReferenceType.BugReport, bugId)
            .Single(item => item.Id == commentId);

        Assert.Equal("Developer note: startup initialization is under review.", comment.Message);
        Assert.Equal("developer", comment.CreatedBy);
        Assert.Equal("developer", comment.UpdatedBy);
        Assert.NotNull(comment.UpdatedAt);

        discussionService.DeleteComment(commentId);

        Assert.Empty(discussionService.GetComments(EntityReferenceType.BugReport, bugId));
    }

    [Fact]
    public void AttachmentService_AddsVideoAttachmentForBugReport()
    {
        var databasePath = CreateTempDatabasePath();
        var attachmentRootPath = CreateTempDirectoryPath();
        using var serviceProvider = CreateServiceProvider(databasePath, attachmentRootPath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var bugService = serviceProvider.GetRequiredService<IBugReportService>();
        var bugId = bugService.CreateBug(new CreateBugReportRequest(
            "Graph freezes",
            "The graph stops updating after changing current limit.",
            "Medium",
            "High",
            "1.2.6",
            "793",
            "tester"));
        var sourceDirectory = CreateTempDirectoryPath();
        var sourceFilePath = Path.Combine(sourceDirectory, "freeze-capture.mp4");
        File.WriteAllBytes(sourceFilePath, [0x00, 0x01, 0x02]);

        var attachmentService = serviceProvider.GetRequiredService<IAttachmentService>();
        var attachmentId = attachmentService.AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.BugReport,
            bugId,
            sourceFilePath,
            "tester"));

        var attachment = attachmentService
            .GetAttachments(EntityReferenceType.BugReport, bugId)
            .Single(item => item.Id == attachmentId);

        Assert.Equal("freeze-capture.mp4", attachment.OriginalFileName);
        Assert.Equal("video/mp4", attachment.ContentType);
        Assert.True(File.Exists(attachment.AbsolutePath));
    }

    [Fact]
    public void ProjectService_AllowsSameTemplateNameInDifferentProjectsAndDeletesProjectData()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var projectService = serviceProvider.GetRequiredService<IProjectService>();
        var firstProjectId = projectService.CreateProject(new CreateProjectRequest("Project A"));
        var secondProjectId = projectService.CreateProject(new CreateProjectRequest("Project B"));
        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();

        managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Shared Template",
            "First project template.",
            RevisionIsRequired: false,
            InitialRevisionName: null,
            ProjectId: firstProjectId));
        managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Shared Template",
            "Second project template.",
            RevisionIsRequired: false,
            InitialRevisionName: null,
            ProjectId: secondProjectId));

        var catalogService = serviceProvider.GetRequiredService<ITestSuiteCatalogService>();
        Assert.Contains(catalogService.GetCatalog(firstProjectId), suite => suite.Name == "Shared Template");
        Assert.Contains(catalogService.GetCatalog(secondProjectId), suite => suite.Name == "Shared Template");

        projectService.DeleteProject(firstProjectId);

        Assert.DoesNotContain(projectService.GetProjects(), project => project.Id == firstProjectId);
        Assert.Empty(catalogService.GetCatalog(firstProjectId));
        Assert.Contains(catalogService.GetCatalog(secondProjectId), suite => suite.Name == "Shared Template");
    }

    [Fact]
    public void ManagementService_CreatesRevisionByCopyingSourceRevisionAndUpdatesName()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Revision Copy Suite",
            "Checks revision copy behavior.",
            RevisionIsRequired: true,
            InitialRevisionName: "Revision A"));
        var revisionAId = testSuite.InitialRevisionId!.Value;
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            revisionAId,
            "Normal",
            "Startup"));
        var testCaseId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Power button",
            "The unit powers on."));
        managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Press the power button.",
            "The button toggles the unit state."));

        var revisionBId = managementService.CreateRevision(new CreateTestSuiteRevisionRequest(
            testSuite.TestSuiteId,
            "Revision B",
            revisionAId));
        managementService.UpdateRevision(new UpdateTestSuiteRevisionRequest(
            revisionBId,
            "Revision B Updated"));

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdSuite = catalog.Single(suite => suite.Id == testSuite.TestSuiteId);
        var revisionA = createdSuite.Revisions.Single(revision => revision.Id == revisionAId);
        var revisionB = createdSuite.Revisions.Single(revision => revision.Id == revisionBId);

        Assert.Equal("Revision B Updated", revisionB.Name);
        Assert.Equal("Normal", revisionB.Sections.Single().Name);
        Assert.Equal("Power button", revisionB.Sections.Single().TestCases.Single().Title);
        Assert.NotEqual(revisionA.Sections.Single().Id, revisionB.Sections.Single().Id);
        Assert.NotEqual(revisionA.Sections.Single().TestCases.Single().Id, revisionB.Sections.Single().TestCases.Single().Id);
    }

    [Fact]
    public void ManagementService_DeletesRevisionWithChildTemplateItems()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Revision Delete Suite",
            "Checks revision delete behavior.",
            RevisionIsRequired: true,
            InitialRevisionName: "Revision A"));
        var revisionAId = testSuite.InitialRevisionId!.Value;
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            revisionAId,
            "Normal",
            "Startup"));
        var testCaseId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Power button",
            "The unit powers on."));
        managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Press the power button.",
            "The button toggles the unit state."));

        var revisionBId = managementService.CreateRevision(new CreateTestSuiteRevisionRequest(
            testSuite.TestSuiteId,
            "Revision B",
            revisionAId));

        managementService.DeleteRevision(revisionBId);

        var catalog = serviceProvider.GetRequiredService<ITestSuiteCatalogService>().GetCatalog();
        var createdSuite = catalog.Single(suite => suite.Id == testSuite.TestSuiteId);
        var revisionA = createdSuite.Revisions.Single(revision => revision.Id == revisionAId);

        Assert.DoesNotContain(createdSuite.Revisions, revision => revision.Id == revisionBId);
        Assert.Equal("Normal", revisionA.Sections.Single().Name);
        Assert.Equal("Power button", revisionA.Sections.Single().TestCases.Single().Title);
    }

    [Fact]
    public void TestSessionService_DeletesSessionWithDiscussionSideData()
    {
        var databasePath = CreateTempDatabasePath();
        var attachmentRootPath = CreateTempDirectoryPath();
        using var serviceProvider = CreateServiceProvider(databasePath, attachmentRootPath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Deletable Session Suite",
            "Used to verify session deletion.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Controls",
            "UI"));
        var testCaseId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Toggle output",
            "Output toggles."));
        managementService.CreateTestStep(new CreateTestStepTemplateRequest(
            testCaseId,
            "Click output.",
            "Output state changes."));

        var sessionService = serviceProvider.GetRequiredService<ITestSessionService>();
        var sessionId = sessionService.CreateSession(new CreateTestSessionRequest(
            "Delete me",
            testSuite.TestSuiteId,
            null,
            "1.0",
            "100",
            "",
            "tester"));
        var testCaseResultId = sessionService.GetSession(sessionId).Sections.Single().TestCases.Single().Id;
        var discussionService = serviceProvider.GetRequiredService<IDiscussionService>();
        discussionService.AddComment(new AddDiscussionCommentRequest(
            EntityReferenceType.TestCaseResult,
            testCaseResultId,
            "Needs review.",
            "developer"));
        var sourceDirectory = CreateTempDirectoryPath();
        var sourceFilePath = Path.Combine(sourceDirectory, "evidence.png");
        File.WriteAllBytes(sourceFilePath, [0x01, 0x02, 0x03]);
        var attachmentService = serviceProvider.GetRequiredService<IAttachmentService>();
        var attachmentId = attachmentService.AddAttachment(new AddAttachmentRequest(
            EntityReferenceType.TestCaseResult,
            testCaseResultId,
            sourceFilePath,
            "tester"));
        var attachmentPath = attachmentService
            .GetAttachments(EntityReferenceType.TestCaseResult, testCaseResultId)
            .Single(item => item.Id == attachmentId)
            .AbsolutePath;

        sessionService.DeleteSession(sessionId);

        Assert.Empty(sessionService.GetSessions());
        Assert.Empty(discussionService.GetComments(EntityReferenceType.TestCaseResult, testCaseResultId));
        Assert.Empty(attachmentService.GetAttachments(EntityReferenceType.TestCaseResult, testCaseResultId));
        Assert.False(File.Exists(attachmentPath));
    }

    [Fact]
    public void CustomFieldDefinitionService_AppliesOneFieldToMultipleScopes()
    {
        var databasePath = CreateTempDatabasePath();
        using var serviceProvider = CreateServiceProvider(databasePath);
        serviceProvider.GetRequiredService<IDatabaseInitializer>().Initialize();

        var managementService = serviceProvider.GetRequiredService<ITestSuiteManagementService>();
        var testSuite = managementService.CreateTestSuite(new CreateTestSuiteRequest(
            "Multi Field Suite",
            "Used to verify field multi-binding.",
            RevisionIsRequired: false,
            InitialRevisionName: null));
        var sectionId = managementService.CreateSection(new CreateTemplateSectionRequest(
            testSuite.TestSuiteId,
            TestSuiteRevisionId: null,
            "Main",
            "UI"));
        var firstCaseTemplateId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "First case",
            "First expected result."));
        var secondCaseTemplateId = managementService.CreateTestCase(new CreateTestCaseTemplateRequest(
            sectionId,
            "Second case",
            "Second expected result."));

        var fieldService = serviceProvider.GetRequiredService<ICustomFieldDefinitionService>();
        var fieldId = fieldService.CreateDefinition(new CreateCustomFieldDefinitionRequest(
            EntityReferenceType.TestCaseResult,
            "Date",
            FieldType.Date,
            IsRequired: false,
            SortOrder: 0,
            ScopeType: CustomFieldScopeType.Global,
            ScopeEntityId: null,
            ScopeDisplayName: "Whole project",
            Options: [],
            Scopes:
            [
                new CustomFieldDefinitionScopeRequest(
                    CustomFieldScopeType.TestCaseTemplate,
                    firstCaseTemplateId,
                    "First case"),
                new CustomFieldDefinitionScopeRequest(
                    CustomFieldScopeType.TestCaseTemplate,
                    secondCaseTemplateId,
                    "Second case")
            ]));

        var definition = fieldService.GetDefinitions().Single(field => field.Id == fieldId);
        var valueService = serviceProvider.GetRequiredService<ICustomFieldValueService>();
        var firstValues = valueService.GetValues(
            EntityReferenceType.TestCaseResult,
            Guid.Empty,
            [new CustomFieldValueScopeItem(CustomFieldScopeType.TestCaseTemplate, firstCaseTemplateId)]);
        var secondValues = valueService.GetValues(
            EntityReferenceType.TestCaseResult,
            Guid.Empty,
            [new CustomFieldValueScopeItem(CustomFieldScopeType.TestCaseTemplate, secondCaseTemplateId)]);

        Assert.Equal(2, definition.Scopes.Count);
        Assert.Contains(firstValues, field => field.FieldDefinitionId == fieldId);
        Assert.Contains(secondValues, field => field.FieldDefinitionId == fieldId);
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
