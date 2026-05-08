using System.Collections.ObjectModel;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class TestSuitesViewModel : ObservableObject
{
    private readonly ITestSuiteCatalogService catalogService;
    private readonly ITestSuiteManagementService testSuiteManagementService;

    public TestSuitesViewModel(
        ITestSuiteCatalogService catalogService,
        ITestSuiteManagementService testSuiteManagementService)
    {
        this.catalogService = catalogService;
        this.testSuiteManagementService = testSuiteManagementService;

        TestSuites = [];
        LoadCatalog();
    }

    public ObservableCollection<TestSuiteItemViewModel> TestSuites { get; }

    [ObservableProperty]
    private TestSuiteItemViewModel? selectedTestSuite;

    [ObservableProperty]
    private TestSuiteRevisionItemViewModel? selectedRevision;

    [ObservableProperty]
    private TemplateSectionItemViewModel? selectedSection;

    [ObservableProperty]
    private TestCaseTemplateItemViewModel? selectedTestCase;

    [ObservableProperty]
    private string newTestSuiteName = string.Empty;

    [ObservableProperty]
    private string newTestSuiteDescription = string.Empty;

    [ObservableProperty]
    private bool newTestSuiteRevisionIsRequired;

    [ObservableProperty]
    private string newTestSuiteInitialRevisionName = string.Empty;

    [ObservableProperty]
    private string newSectionName = string.Empty;

    [ObservableProperty]
    private string newSectionCategory = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    partial void OnSelectedTestSuiteChanged(TestSuiteItemViewModel? value)
    {
        SelectedRevision = value?.Revisions.FirstOrDefault();
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRevisionChanged(TestSuiteRevisionItemViewModel? value)
    {
        SelectedSection = value?.Sections.FirstOrDefault();
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSectionChanged(TemplateSectionItemViewModel? value)
    {
        SelectedTestCase = value?.TestCases.FirstOrDefault();
    }

    partial void OnNewTestSuiteNameChanged(string value)
    {
        CreateTestSuiteCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewTestSuiteRevisionIsRequiredChanged(bool value)
    {
        CreateTestSuiteCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewTestSuiteInitialRevisionNameChanged(string value)
    {
        CreateTestSuiteCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewSectionNameChanged(string value)
    {
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanCreateTestSuite))]
    private void CreateTestSuite()
    {
        try
        {
            var result = testSuiteManagementService.CreateTestSuite(new CreateTestSuiteRequest(
                NewTestSuiteName,
                NewTestSuiteDescription,
                NewTestSuiteRevisionIsRequired,
                NewTestSuiteInitialRevisionName));

            NewTestSuiteName = string.Empty;
            NewTestSuiteDescription = string.Empty;
            NewTestSuiteRevisionIsRequired = false;
            NewTestSuiteInitialRevisionName = string.Empty;

            LoadCatalog(result.TestSuiteId, result.InitialRevisionId);
            StatusMessage = "Test suite created.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateSection))]
    private void CreateSection()
    {
        if (SelectedTestSuite is null)
        {
            return;
        }

        var selectedRevisionId = SelectedTestSuite.RevisionIsRequired
            ? SelectedRevision?.Id
            : null;

        try
        {
            var sectionId = testSuiteManagementService.CreateSection(new CreateTemplateSectionRequest(
                SelectedTestSuite.Id,
                selectedRevisionId,
                NewSectionName,
                NewSectionCategory));

            NewSectionName = string.Empty;
            NewSectionCategory = string.Empty;

            LoadCatalog(SelectedTestSuite.Id, selectedRevisionId, sectionId);
            StatusMessage = "Section created.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private bool CanCreateTestSuite()
    {
        return !string.IsNullOrWhiteSpace(NewTestSuiteName)
            && (!NewTestSuiteRevisionIsRequired || !string.IsNullOrWhiteSpace(NewTestSuiteInitialRevisionName));
    }

    private bool CanCreateSection()
    {
        if (SelectedTestSuite is null || string.IsNullOrWhiteSpace(NewSectionName))
        {
            return false;
        }

        return !SelectedTestSuite.RevisionIsRequired
            || (SelectedRevision is not null && SelectedRevision.Id != Guid.Empty);
    }

    private void LoadCatalog(Guid? selectedTestSuiteId = null, Guid? selectedRevisionId = null, Guid? selectedSectionId = null)
    {
        var testSuites = catalogService.GetCatalog()
            .Select(MapTestSuite)
            .ToList();

        TestSuites.Clear();
        foreach (var testSuite in testSuites)
        {
            TestSuites.Add(testSuite);
        }

        SelectedTestSuite = selectedTestSuiteId is null
            ? TestSuites.FirstOrDefault()
            : TestSuites.FirstOrDefault(testSuite => testSuite.Id == selectedTestSuiteId) ?? TestSuites.FirstOrDefault();

        SelectedRevision = selectedRevisionId is null
            ? SelectedTestSuite?.Revisions.FirstOrDefault()
            : SelectedTestSuite?.Revisions.FirstOrDefault(revision => revision.Id == selectedRevisionId)
                ?? SelectedTestSuite?.Revisions.FirstOrDefault();

        SelectedSection = selectedSectionId is null
            ? SelectedRevision?.Sections.FirstOrDefault()
            : SelectedRevision?.Sections.FirstOrDefault(section => section.Id == selectedSectionId)
                ?? SelectedRevision?.Sections.FirstOrDefault();
    }

    private static TestSuiteItemViewModel MapTestSuite(TestSuiteCatalogItem testSuite)
    {
        return new TestSuiteItemViewModel(
            testSuite.Id,
            testSuite.Name,
            testSuite.Description,
            testSuite.RevisionIsRequired,
            testSuite.Revisions.Select(MapRevision));
    }

    private static TestSuiteRevisionItemViewModel MapRevision(TestSuiteRevisionCatalogItem revision)
    {
        return new TestSuiteRevisionItemViewModel(
            revision.Id,
            revision.Name,
            revision.EffectiveDate,
            revision.Sections
                .OrderBy(section => section.SortOrder)
                .Select(MapSection));
    }

    private static TemplateSectionItemViewModel MapSection(TemplateSectionCatalogItem section)
    {
        return new TemplateSectionItemViewModel(
            section.Id,
            section.Name,
            section.Category,
            section.SortOrder,
            section.TestCases
                .OrderBy(testCase => testCase.SortOrder)
                .Select(MapTestCase));
    }

    private static TestCaseTemplateItemViewModel MapTestCase(TestCaseTemplateCatalogItem testCase)
    {
        return new TestCaseTemplateItemViewModel(
            testCase.Id,
            testCase.Title,
            testCase.ExpectedResult,
            testCase.SortOrder,
            testCase.Steps
                .OrderBy(step => step.SortOrder)
                .Select(MapStep));
    }

    private static TestStepTemplateItemViewModel MapStep(TestStepTemplateCatalogItem step)
    {
        return new TestStepTemplateItemViewModel(
            step.Id,
            step.StepText,
            step.ExpectedResult,
            step.SortOrder);
    }
}
