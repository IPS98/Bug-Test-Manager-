using System.Collections.ObjectModel;
using System.Windows;
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
    private GridLength revisionColumnWidth = new(0);

    [ObservableProperty]
    private Visibility revisionColumnVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private TemplateDialogKind activeDialog;

    [ObservableProperty]
    private string dialogTitle = string.Empty;

    [ObservableProperty]
    private Visibility dialogOverlayVisibility = Visibility.Collapsed;

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
    private string newTestCaseTitle = string.Empty;

    [ObservableProperty]
    private string newTestCaseExpectedResult = string.Empty;

    [ObservableProperty]
    private string newStepText = string.Empty;

    [ObservableProperty]
    private string newStepExpectedResult = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    partial void OnSelectedTestSuiteChanged(TestSuiteItemViewModel? value)
    {
        SelectedRevision = value?.Revisions.FirstOrDefault();
        UpdateRevisionColumn(value);
        ShowCreateSectionDialogCommand.NotifyCanExecuteChanged();
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRevisionChanged(TestSuiteRevisionItemViewModel? value)
    {
        SelectedSection = value?.Sections.FirstOrDefault();
        ShowCreateSectionDialogCommand.NotifyCanExecuteChanged();
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSectionChanged(TemplateSectionItemViewModel? value)
    {
        SelectedTestCase = value?.TestCases.FirstOrDefault();
        ShowCreateTestCaseDialogCommand.NotifyCanExecuteChanged();
        CreateTestCaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTestCaseChanged(TestCaseTemplateItemViewModel? value)
    {
        ShowCreateTestStepDialogCommand.NotifyCanExecuteChanged();
        CreateTestStepCommand.NotifyCanExecuteChanged();
    }

    partial void OnActiveDialogChanged(TemplateDialogKind value)
    {
        DialogOverlayVisibility = value == TemplateDialogKind.None
            ? Visibility.Collapsed
            : Visibility.Visible;

        DialogTitle = value switch
        {
            TemplateDialogKind.TestSuite => "Add Test Suite",
            TemplateDialogKind.Section => "Add Section",
            TemplateDialogKind.TestCase => "Add Test Case",
            TemplateDialogKind.Step => "Add Step",
            _ => string.Empty
        };
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

    partial void OnNewTestCaseTitleChanged(string value)
    {
        CreateTestCaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewStepTextChanged(string value)
    {
        CreateTestStepCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ShowCreateTestSuiteDialog()
    {
        NewTestSuiteName = string.Empty;
        NewTestSuiteDescription = string.Empty;
        NewTestSuiteRevisionIsRequired = false;
        NewTestSuiteInitialRevisionName = string.Empty;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.TestSuite;
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateSectionDialog))]
    private void ShowCreateSectionDialog()
    {
        NewSectionName = string.Empty;
        NewSectionCategory = string.Empty;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.Section;
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateTestCaseDialog))]
    private void ShowCreateTestCaseDialog()
    {
        NewTestCaseTitle = string.Empty;
        NewTestCaseExpectedResult = string.Empty;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.TestCase;
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateTestStepDialog))]
    private void ShowCreateTestStepDialog()
    {
        NewStepText = string.Empty;
        NewStepExpectedResult = string.Empty;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.Step;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        ActiveDialog = TemplateDialogKind.None;
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
            ActiveDialog = TemplateDialogKind.None;
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
            ActiveDialog = TemplateDialogKind.None;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateTestCase))]
    private void CreateTestCase()
    {
        if (SelectedTestSuite is null || SelectedSection is null)
        {
            return;
        }

        var selectedRevisionId = GetSelectedRevisionIdForSave();

        try
        {
            var testCaseId = testSuiteManagementService.CreateTestCase(new CreateTestCaseTemplateRequest(
                SelectedSection.Id,
                NewTestCaseTitle,
                NewTestCaseExpectedResult));

            NewTestCaseTitle = string.Empty;
            NewTestCaseExpectedResult = string.Empty;

            LoadCatalog(SelectedTestSuite.Id, selectedRevisionId, SelectedSection.Id, testCaseId);
            StatusMessage = "Test case created.";
            ActiveDialog = TemplateDialogKind.None;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateTestStep))]
    private void CreateTestStep()
    {
        if (SelectedTestSuite is null || SelectedSection is null || SelectedTestCase is null)
        {
            return;
        }

        var selectedRevisionId = GetSelectedRevisionIdForSave();

        try
        {
            testSuiteManagementService.CreateTestStep(new CreateTestStepTemplateRequest(
                SelectedTestCase.Id,
                NewStepText,
                NewStepExpectedResult));

            NewStepText = string.Empty;
            NewStepExpectedResult = string.Empty;

            LoadCatalog(SelectedTestSuite.Id, selectedRevisionId, SelectedSection.Id, SelectedTestCase.Id);
            StatusMessage = "Step created.";
            ActiveDialog = TemplateDialogKind.None;
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

    private bool CanCreateTestCase()
    {
        return SelectedSection is not null && !string.IsNullOrWhiteSpace(NewTestCaseTitle);
    }

    private bool CanCreateTestStep()
    {
        return SelectedTestCase is not null && !string.IsNullOrWhiteSpace(NewStepText);
    }

    private bool CanShowCreateSectionDialog()
    {
        if (SelectedTestSuite is null)
        {
            return false;
        }

        return !SelectedTestSuite.RevisionIsRequired
            || (SelectedRevision is not null && SelectedRevision.Id != Guid.Empty);
    }

    private bool CanShowCreateTestCaseDialog()
    {
        return SelectedSection is not null;
    }

    private bool CanShowCreateTestStepDialog()
    {
        return SelectedTestCase is not null;
    }

    private void UpdateRevisionColumn(TestSuiteItemViewModel? selectedSuite)
    {
        var shouldShowRevisionColumn = selectedSuite?.RevisionIsRequired == true;

        RevisionColumnWidth = shouldShowRevisionColumn
            ? new GridLength(240)
            : new GridLength(0);
        RevisionColumnVisibility = shouldShowRevisionColumn
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private Guid? GetSelectedRevisionIdForSave()
    {
        return SelectedTestSuite?.RevisionIsRequired == true
            ? SelectedRevision?.Id
            : null;
    }

    private void LoadCatalog(
        Guid? selectedTestSuiteId = null,
        Guid? selectedRevisionId = null,
        Guid? selectedSectionId = null,
        Guid? selectedTestCaseId = null)
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

        SelectedTestCase = selectedTestCaseId is null
            ? SelectedSection?.TestCases.FirstOrDefault()
            : SelectedSection?.TestCases.FirstOrDefault(testCase => testCase.Id == selectedTestCaseId)
                ?? SelectedSection?.TestCases.FirstOrDefault();
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
