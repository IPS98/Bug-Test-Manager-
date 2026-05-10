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
    private TestStepTemplateItemViewModel? selectedStep;

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
    private TemplateDeleteTarget pendingDeleteTarget;

    [ObservableProperty]
    private Guid pendingDeleteId;

    [ObservableProperty]
    private Guid editingItemId;

    [ObservableProperty]
    private string deleteConfirmationMessage = string.Empty;

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
        ShowDeleteTestSuiteDialogCommand.NotifyCanExecuteChanged();
        ShowCreateSectionDialogCommand.NotifyCanExecuteChanged();
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRevisionChanged(TestSuiteRevisionItemViewModel? value)
    {
        SelectedSection = value?.Sections.FirstOrDefault();
        ShowDeleteSectionDialogCommand.NotifyCanExecuteChanged();
        ShowCreateSectionDialogCommand.NotifyCanExecuteChanged();
        CreateSectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSectionChanged(TemplateSectionItemViewModel? value)
    {
        SelectedTestCase = value?.TestCases.FirstOrDefault();
        ShowDeleteSectionDialogCommand.NotifyCanExecuteChanged();
        ShowCreateTestCaseDialogCommand.NotifyCanExecuteChanged();
        ShowDeleteTestCaseDialogCommand.NotifyCanExecuteChanged();
        CreateTestCaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTestCaseChanged(TestCaseTemplateItemViewModel? value)
    {
        SelectedStep = value?.Steps.FirstOrDefault();
        ShowDeleteTestCaseDialogCommand.NotifyCanExecuteChanged();
        ShowCreateTestStepDialogCommand.NotifyCanExecuteChanged();
        ShowDeleteTestStepDialogCommand.NotifyCanExecuteChanged();
        CreateTestStepCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedStepChanged(TestStepTemplateItemViewModel? value)
    {
        ShowDeleteTestStepDialogCommand.NotifyCanExecuteChanged();
    }

    partial void OnActiveDialogChanged(TemplateDialogKind value)
    {
        DialogOverlayVisibility = value == TemplateDialogKind.None
            ? Visibility.Collapsed
            : Visibility.Visible;

        DialogTitle = value switch
        {
            TemplateDialogKind.TestSuite => "Add Test Suite",
            TemplateDialogKind.EditTestSuite => "Edit Test Suite",
            TemplateDialogKind.Section => "Add Section",
            TemplateDialogKind.EditSection => "Edit Section",
            TemplateDialogKind.TestCase => "Add Test Case",
            TemplateDialogKind.EditTestCase => "Edit Test Case",
            TemplateDialogKind.Step => "Add Check",
            TemplateDialogKind.EditStep => "Edit Check",
            TemplateDialogKind.DeleteConfirmation => "Confirm Delete",
            _ => string.Empty
        };

        SaveEditCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditingItemIdChanged(Guid value)
    {
        SaveEditCommand.NotifyCanExecuteChanged();
    }

    partial void OnPendingDeleteTargetChanged(TemplateDeleteTarget value)
    {
        ConfirmDeleteCommand.NotifyCanExecuteChanged();
    }

    partial void OnPendingDeleteIdChanged(Guid value)
    {
        ConfirmDeleteCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewTestSuiteNameChanged(string value)
    {
        CreateTestSuiteCommand.NotifyCanExecuteChanged();
        SaveEditCommand.NotifyCanExecuteChanged();
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
        SaveEditCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewTestCaseTitleChanged(string value)
    {
        CreateTestCaseCommand.NotifyCanExecuteChanged();
        SaveEditCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewStepTextChanged(string value)
    {
        CreateTestStepCommand.NotifyCanExecuteChanged();
        SaveEditCommand.NotifyCanExecuteChanged();
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
        ClearPendingDelete();
        ClearEditing();
    }

    [RelayCommand(CanExecute = nameof(CanShowEditTestSuiteDialog))]
    private void ShowEditTestSuiteDialog(TestSuiteItemViewModel? testSuite)
    {
        if (testSuite is null)
        {
            return;
        }

        SelectedTestSuite = testSuite;
        EditingItemId = testSuite.Id;
        NewTestSuiteName = testSuite.Name;
        NewTestSuiteDescription = testSuite.Description;
        NewTestSuiteRevisionIsRequired = testSuite.RevisionIsRequired;
        NewTestSuiteInitialRevisionName = string.Empty;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.EditTestSuite;
    }

    [RelayCommand(CanExecute = nameof(CanShowEditSectionDialog))]
    private void ShowEditSectionDialog(TemplateSectionItemViewModel? section)
    {
        if (section is null)
        {
            return;
        }

        SelectedSection = section;
        EditingItemId = section.Id;
        NewSectionName = section.Name;
        NewSectionCategory = section.Category;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.EditSection;
    }

    [RelayCommand(CanExecute = nameof(CanShowEditTestCaseDialog))]
    private void ShowEditTestCaseDialog(TestCaseTemplateItemViewModel? testCase)
    {
        if (testCase is null)
        {
            return;
        }

        SelectedTestCase = testCase;
        EditingItemId = testCase.Id;
        NewTestCaseTitle = testCase.Title;
        NewTestCaseExpectedResult = testCase.ExpectedResult;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.EditTestCase;
    }

    [RelayCommand(CanExecute = nameof(CanShowEditTestStepDialog))]
    private void ShowEditTestStepDialog(TestStepTemplateItemViewModel? step)
    {
        if (step is null)
        {
            return;
        }

        SelectedStep = step;
        EditingItemId = step.Id;
        NewStepText = step.StepText;
        NewStepExpectedResult = step.ExpectedResult;
        StatusMessage = "Ready";
        ActiveDialog = TemplateDialogKind.EditStep;
    }

    [RelayCommand(CanExecute = nameof(CanShowDeleteTestSuiteDialog))]
    private void ShowDeleteTestSuiteDialog(TestSuiteItemViewModel? testSuite)
    {
        if (testSuite is null)
        {
            return;
        }

        SelectedTestSuite = testSuite;
        ShowDeleteConfirmation(
            TemplateDeleteTarget.TestSuite,
            testSuite.Id,
            $"Delete all of test suite \"{testSuite.Name}\"? This will also delete its revisions, sections, test cases, and checks.");
    }

    [RelayCommand(CanExecute = nameof(CanShowDeleteSectionDialog))]
    private void ShowDeleteSectionDialog(TemplateSectionItemViewModel? section)
    {
        if (section is null)
        {
            return;
        }

        SelectedSection = section;
        ShowDeleteConfirmation(
            TemplateDeleteTarget.Section,
            section.Id,
            $"Delete section \"{section.Name}\"? This will also delete its test cases and checks.");
    }

    [RelayCommand(CanExecute = nameof(CanShowDeleteTestCaseDialog))]
    private void ShowDeleteTestCaseDialog(TestCaseTemplateItemViewModel? testCase)
    {
        if (testCase is null)
        {
            return;
        }

        SelectedTestCase = testCase;
        ShowDeleteConfirmation(
            TemplateDeleteTarget.TestCase,
            testCase.Id,
            $"Delete test case \"{testCase.Title}\"? This will also delete its checks.");
    }

    [RelayCommand(CanExecute = nameof(CanShowDeleteTestStepDialog))]
    private void ShowDeleteTestStepDialog(TestStepTemplateItemViewModel? step)
    {
        if (step is null)
        {
            return;
        }

        SelectedStep = step;
        ShowDeleteConfirmation(
            TemplateDeleteTarget.Step,
            step.Id,
            $"Delete check {step.SortOrder}: \"{step.StepText}\"?");
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
            StatusMessage = "Check created.";
            ActiveDialog = TemplateDialogKind.None;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirmDelete))]
    private void ConfirmDelete()
    {
        var testSuiteId = SelectedTestSuite?.Id;
        var revisionId = GetSelectedRevisionIdForSave();
        var sectionId = SelectedSection?.Id;
        var testCaseId = SelectedTestCase?.Id;
        var target = PendingDeleteTarget;
        var id = PendingDeleteId;

        try
        {
            switch (target)
            {
                case TemplateDeleteTarget.TestSuite:
                    testSuiteManagementService.DeleteTestSuite(id);
                    LoadCatalog();
                    StatusMessage = "Test suite deleted.";
                    break;
                case TemplateDeleteTarget.Section:
                    testSuiteManagementService.DeleteSection(id);
                    LoadCatalog(testSuiteId, revisionId);
                    StatusMessage = "Section deleted.";
                    break;
                case TemplateDeleteTarget.TestCase:
                    testSuiteManagementService.DeleteTestCase(id);
                    LoadCatalog(testSuiteId, revisionId, sectionId);
                    StatusMessage = "Test case deleted.";
                    break;
                case TemplateDeleteTarget.Step:
                    testSuiteManagementService.DeleteTestStep(id);
                    LoadCatalog(testSuiteId, revisionId, sectionId, testCaseId);
                    StatusMessage = "Check deleted.";
                    break;
                default:
                    return;
            }

            ActiveDialog = TemplateDialogKind.None;
            ClearPendingDelete();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveEdit))]
    private void SaveEdit()
    {
        var testSuiteId = SelectedTestSuite?.Id;
        var revisionId = GetSelectedRevisionIdForSave();
        var sectionId = SelectedSection?.Id;
        var testCaseId = SelectedTestCase?.Id;
        var stepId = SelectedStep?.Id;

        try
        {
            switch (ActiveDialog)
            {
                case TemplateDialogKind.EditTestSuite:
                    testSuiteManagementService.UpdateTestSuite(new UpdateTestSuiteRequest(
                        EditingItemId,
                        NewTestSuiteName,
                        NewTestSuiteDescription));
                    LoadCatalog(EditingItemId, revisionId);
                    StatusMessage = "Test suite updated.";
                    break;
                case TemplateDialogKind.EditSection:
                    testSuiteManagementService.UpdateSection(new UpdateTemplateSectionRequest(
                        EditingItemId,
                        NewSectionName,
                        NewSectionCategory));
                    LoadCatalog(testSuiteId, revisionId, EditingItemId);
                    StatusMessage = "Section updated.";
                    break;
                case TemplateDialogKind.EditTestCase:
                    testSuiteManagementService.UpdateTestCase(new UpdateTestCaseTemplateRequest(
                        EditingItemId,
                        NewTestCaseTitle,
                        NewTestCaseExpectedResult));
                    LoadCatalog(testSuiteId, revisionId, sectionId, EditingItemId);
                    StatusMessage = "Test case updated.";
                    break;
                case TemplateDialogKind.EditStep:
                    testSuiteManagementService.UpdateTestStep(new UpdateTestStepTemplateRequest(
                        EditingItemId,
                        NewStepText,
                        NewStepExpectedResult));
                    LoadCatalog(testSuiteId, revisionId, sectionId, testCaseId, stepId);
                    StatusMessage = "Check updated.";
                    break;
                default:
                    return;
            }

            ActiveDialog = TemplateDialogKind.None;
            ClearEditing();
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

    private bool CanShowEditTestSuiteDialog(TestSuiteItemViewModel? testSuite)
    {
        return testSuite is not null;
    }

    private bool CanShowEditSectionDialog(TemplateSectionItemViewModel? section)
    {
        return section is not null;
    }

    private bool CanShowEditTestCaseDialog(TestCaseTemplateItemViewModel? testCase)
    {
        return testCase is not null;
    }

    private bool CanShowEditTestStepDialog(TestStepTemplateItemViewModel? step)
    {
        return step is not null;
    }

    private bool CanShowDeleteTestSuiteDialog(TestSuiteItemViewModel? testSuite)
    {
        return testSuite is not null;
    }

    private bool CanShowDeleteSectionDialog(TemplateSectionItemViewModel? section)
    {
        return section is not null;
    }

    private bool CanShowDeleteTestCaseDialog(TestCaseTemplateItemViewModel? testCase)
    {
        return testCase is not null;
    }

    private bool CanShowDeleteTestStepDialog(TestStepTemplateItemViewModel? step)
    {
        return step is not null;
    }

    private bool CanConfirmDelete()
    {
        return PendingDeleteTarget != TemplateDeleteTarget.None && PendingDeleteId != Guid.Empty;
    }

    private bool CanSaveEdit()
    {
        if (EditingItemId == Guid.Empty)
        {
            return false;
        }

        return ActiveDialog switch
        {
            TemplateDialogKind.EditTestSuite => !string.IsNullOrWhiteSpace(NewTestSuiteName),
            TemplateDialogKind.EditSection => !string.IsNullOrWhiteSpace(NewSectionName),
            TemplateDialogKind.EditTestCase => !string.IsNullOrWhiteSpace(NewTestCaseTitle),
            TemplateDialogKind.EditStep => !string.IsNullOrWhiteSpace(NewStepText),
            _ => false
        };
    }

    private void ShowDeleteConfirmation(TemplateDeleteTarget target, Guid id, string message)
    {
        PendingDeleteTarget = target;
        PendingDeleteId = id;
        DeleteConfirmationMessage = message;
        StatusMessage = "This action cannot be undone.";
        ActiveDialog = TemplateDialogKind.DeleteConfirmation;
    }

    private void ClearPendingDelete()
    {
        PendingDeleteTarget = TemplateDeleteTarget.None;
        PendingDeleteId = Guid.Empty;
        DeleteConfirmationMessage = string.Empty;
    }

    private void ClearEditing()
    {
        EditingItemId = Guid.Empty;
    }

    private void UpdateRevisionColumn(TestSuiteItemViewModel? selectedSuite)
    {
        var shouldShowRevisionColumn = selectedSuite?.RevisionIsRequired == true;

        RevisionColumnWidth = shouldShowRevisionColumn
            ? new GridLength(280)
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
        Guid? selectedTestCaseId = null,
        Guid? selectedStepId = null)
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

        SelectedStep = selectedStepId is null
            ? SelectedTestCase?.Steps.FirstOrDefault()
            : SelectedTestCase?.Steps.FirstOrDefault(step => step.Id == selectedStepId)
                ?? SelectedTestCase?.Steps.FirstOrDefault();
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
