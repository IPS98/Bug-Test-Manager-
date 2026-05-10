using System.Collections.ObjectModel;
using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class TestSessionsViewModel : ObservableObject
{
    private readonly ITestSessionService testSessionService;
    private readonly ITestSuiteCatalogService testSuiteCatalogService;
    private readonly IAttachmentService attachmentService;
    private readonly IFilePickerService filePickerService;
    private readonly IUserContext userContext;

    public TestSessionsViewModel(
        ITestSessionService testSessionService,
        ITestSuiteCatalogService testSuiteCatalogService,
        IAttachmentService attachmentService,
        IFilePickerService filePickerService,
        IUserContext userContext)
    {
        this.testSessionService = testSessionService;
        this.testSuiteCatalogService = testSuiteCatalogService;
        this.attachmentService = attachmentService;
        this.filePickerService = filePickerService;
        this.userContext = userContext;
        TestSuites = [];
        Revisions = [];
        Sessions = [];
        Sections = [];
        FilteredTestCases = [];
        ResultAttachments = [];
        ResultStatuses = Enum.GetValues<TestResultStatus>()
            .Select(status => new SelectionOption<TestResultStatus>(
                status,
                TestResultStatusDisplayNames.ForStatus(status)))
            .ToList();
        ResultStatusFilters =
        [
            new SelectionOption<TestResultStatus?>(null, "All results"),
            .. Enum.GetValues<TestResultStatus>()
                .Select(status => new SelectionOption<TestResultStatus?>(
                    status,
                    TestResultStatusDisplayNames.ForStatus(status)))
        ];
        SelectedResultStatus = ResultStatuses.FirstOrDefault();
        SelectedResultStatusFilter = ResultStatusFilters.FirstOrDefault();
        Refresh();
    }

    public ObservableCollection<TestSessionSuiteOption> TestSuites { get; }

    public ObservableCollection<TestSessionRevisionOption> Revisions { get; }

    public ObservableCollection<TestSessionSummaryViewModel> Sessions { get; }

    public ObservableCollection<TestSectionResultViewModel> Sections { get; }

    public ObservableCollection<TestCaseResultViewModel> FilteredTestCases { get; }

    public ObservableCollection<AttachmentItemViewModel> ResultAttachments { get; }

    public IReadOnlyList<SelectionOption<TestResultStatus>> ResultStatuses { get; }

    public IReadOnlyList<SelectionOption<TestResultStatus?>> ResultStatusFilters { get; }

    [ObservableProperty]
    private TestSessionSuiteOption? selectedTestSuite;

    [ObservableProperty]
    private TestSessionRevisionOption? selectedRevision;

    [ObservableProperty]
    private TestSessionSummaryViewModel? selectedSession;

    [ObservableProperty]
    private TestSectionResultViewModel? selectedSection;

    [ObservableProperty]
    private TestCaseResultViewModel? selectedTestCase;

    [ObservableProperty]
    private TestStepResultViewModel? selectedStep;

    [ObservableProperty]
    private SelectionOption<TestResultStatus?>? selectedResultStatusFilter;

    [ObservableProperty]
    private string newSessionName = string.Empty;

    [ObservableProperty]
    private string testedVersion = string.Empty;

    [ObservableProperty]
    private string buildNumber = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string resultSummary = "No session selected.";

    [ObservableProperty]
    private TestSessionResultTargetKind editingResultTarget;

    [ObservableProperty]
    private Guid editingResultId;

    [ObservableProperty]
    private string resultDialogTitle = string.Empty;

    [ObservableProperty]
    private Visibility resultDialogOverlayVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private SelectionOption<TestResultStatus>? selectedResultStatus;

    [ObservableProperty]
    private string resultComment = string.Empty;

    partial void OnSelectedTestSuiteChanged(TestSessionSuiteOption? value)
    {
        Revisions.Clear();

        if (value is not null)
        {
            foreach (var revision in value.Revisions)
            {
                Revisions.Add(revision);
            }
        }

        SelectedRevision = Revisions.FirstOrDefault();
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRevisionChanged(TestSessionRevisionOption? value)
    {
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSessionChanged(TestSessionSummaryViewModel? value)
    {
        LoadSelectedSession(value?.Id);
    }

    partial void OnSelectedSectionChanged(TestSectionResultViewModel? value)
    {
        RefreshFilteredTestCases();
    }

    partial void OnSelectedTestCaseChanged(TestCaseResultViewModel? value)
    {
        SelectedStep = value?.Steps.FirstOrDefault();
        ShowEditTestCaseResultCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedStepChanged(TestStepResultViewModel? value)
    {
        ShowEditTestStepResultCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewSessionNameChanged(string value)
    {
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditingResultTargetChanged(TestSessionResultTargetKind value)
    {
        ResultDialogOverlayVisibility = value == TestSessionResultTargetKind.None
            ? Visibility.Collapsed
            : Visibility.Visible;

        SaveResultCommand.NotifyCanExecuteChanged();
        AddAttachmentCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditingResultIdChanged(Guid value)
    {
        SaveResultCommand.NotifyCanExecuteChanged();
        AddAttachmentCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedResultStatusChanged(SelectionOption<TestResultStatus>? value)
    {
        SaveResultCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedResultStatusFilterChanged(SelectionOption<TestResultStatus?>? value)
    {
        RefreshFilteredTestCases(SelectedTestCase?.Id);
    }

    public void Refresh()
    {
        LoadTestSuites();
        LoadSessions(SelectedSession?.Id);
    }

    [RelayCommand(CanExecute = nameof(CanCreateSession))]
    private void CreateSession()
    {
        if (SelectedTestSuite is null)
        {
            return;
        }

        try
        {
            var sessionId = testSessionService.CreateSession(new CreateTestSessionRequest(
                NewSessionName,
                SelectedTestSuite.Id,
                SelectedRevision?.Id,
                TestedVersion,
                BuildNumber,
                Notes,
                userContext.UserName));

            NewSessionName = string.Empty;
            TestedVersion = string.Empty;
            BuildNumber = string.Empty;
            Notes = string.Empty;

            LoadSessions(sessionId);
            StatusMessage = "Test session created from template.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowEditTestCaseResult))]
    private void ShowEditTestCaseResult(TestCaseResultViewModel? testCase)
    {
        if (testCase is null)
        {
            return;
        }

        SelectedTestCase = testCase;
        EditingResultId = testCase.Id;
        SelectedResultStatus = ResultStatuses.Single(option => option.Value == testCase.Status);
        ResultComment = testCase.Comment;
        ResultDialogTitle = $"Update Test Case: {testCase.Title}";
        StatusMessage = "Ready";
        EditingResultTarget = TestSessionResultTargetKind.TestCase;
        LoadResultAttachments();
    }

    [RelayCommand(CanExecute = nameof(CanShowEditTestStepResult))]
    private void ShowEditTestStepResult(TestStepResultViewModel? step)
    {
        if (step is null)
        {
            return;
        }

        SelectedStep = step;
        EditingResultId = step.Id;
        SelectedResultStatus = ResultStatuses.Single(option => option.Value == step.Status);
        ResultComment = step.Comment;
        ResultDialogTitle = $"Update Check {step.SortOrder}";
        StatusMessage = "Ready";
        EditingResultTarget = TestSessionResultTargetKind.Step;
        LoadResultAttachments();
    }

    [RelayCommand]
    private void CloseResultDialog()
    {
        EditingResultTarget = TestSessionResultTargetKind.None;
        EditingResultId = Guid.Empty;
        ResultDialogTitle = string.Empty;
        ResultComment = string.Empty;
        SelectedResultStatus = ResultStatuses.FirstOrDefault();
        ResultAttachments.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanAddAttachment))]
    private void AddAttachment()
    {
        var sourceFilePath = filePickerService.PickAttachmentFile();
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return;
        }

        try
        {
            attachmentService.AddAttachment(new AddAttachmentRequest(
                GetEditingEntityType(),
                EditingResultId,
                sourceFilePath,
                userContext.UserName));

            LoadResultAttachments();
            StatusMessage = "Attachment added.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveResult))]
    private void SaveResult()
    {
        if (SelectedResultStatus is null)
        {
            return;
        }

        var sessionId = SelectedSession?.Id;
        var sectionId = SelectedSection?.Id;
        var testCaseId = EditingResultTarget == TestSessionResultTargetKind.TestCase
            ? EditingResultId
            : SelectedTestCase?.Id;
        var stepId = EditingResultTarget == TestSessionResultTargetKind.Step
            ? EditingResultId
            : SelectedStep?.Id;

        try
        {
            switch (EditingResultTarget)
            {
                case TestSessionResultTargetKind.TestCase:
                    testSessionService.UpdateTestCaseResult(new UpdateTestCaseResultRequest(
                        EditingResultId,
                        SelectedResultStatus.Value,
                        ResultComment));
                    StatusMessage = "Test case result updated.";
                    break;
                case TestSessionResultTargetKind.Step:
                    testSessionService.UpdateTestStepResult(new UpdateTestStepResultRequest(
                        EditingResultId,
                        SelectedResultStatus.Value,
                        ResultComment));
                    StatusMessage = "Check result updated.";
                    break;
                default:
                    return;
            }

            CloseResultDialog();
            LoadSessions(sessionId);
            LoadSelectedSession(sessionId, sectionId, testCaseId, stepId);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private bool CanCreateSession()
    {
        return SelectedTestSuite is not null
            && !string.IsNullOrWhiteSpace(NewSessionName)
            && (!SelectedTestSuite.RevisionIsRequired || SelectedRevision?.Id is not null);
    }

    private bool CanShowEditTestCaseResult(TestCaseResultViewModel? testCase)
    {
        return testCase is not null;
    }

    private bool CanShowEditTestStepResult(TestStepResultViewModel? step)
    {
        return step is not null;
    }

    private bool CanSaveResult()
    {
        return EditingResultTarget != TestSessionResultTargetKind.None
            && EditingResultId != Guid.Empty
            && SelectedResultStatus is not null;
    }

    private bool CanAddAttachment()
    {
        return EditingResultTarget != TestSessionResultTargetKind.None
            && EditingResultId != Guid.Empty;
    }

    private void LoadTestSuites()
    {
        var selectedSuiteId = SelectedTestSuite?.Id;
        var suites = testSuiteCatalogService.GetCatalog()
            .Select(suite => new TestSessionSuiteOption(
                suite.Id,
                suite.Name,
                suite.RevisionIsRequired,
                suite.RevisionIsRequired
                    ? suite.Revisions
                        .Select(revision => new TestSessionRevisionOption(revision.Id, revision.Name))
                        .ToList()
                    : [new TestSessionRevisionOption(null, "No revision")]))
            .ToList();

        TestSuites.Clear();
        foreach (var suite in suites)
        {
            TestSuites.Add(suite);
        }

        SelectedTestSuite = selectedSuiteId is null
            ? TestSuites.FirstOrDefault()
            : TestSuites.FirstOrDefault(suite => suite.Id == selectedSuiteId) ?? TestSuites.FirstOrDefault();
    }

    private void LoadSessions(Guid? selectedSessionId = null)
    {
        var preferredSessionId = selectedSessionId ?? SelectedSession?.Id;
        var sessions = testSessionService.GetSessions()
            .Select(MapSession)
            .ToList();

        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }

        SelectedSession = preferredSessionId is null
            ? Sessions.FirstOrDefault()
            : Sessions.FirstOrDefault(session => session.Id == preferredSessionId) ?? Sessions.FirstOrDefault();
    }

    private void LoadSelectedSession(
        Guid? selectedSessionId,
        Guid? selectedSectionId = null,
        Guid? selectedTestCaseId = null,
        Guid? selectedStepId = null)
    {
        Sections.Clear();
        FilteredTestCases.Clear();

        if (selectedSessionId is null)
        {
            SelectedSection = null;
            ResultSummary = "No session selected.";
            return;
        }

        try
        {
            var session = testSessionService.GetSession(selectedSessionId.Value);
            foreach (var section in session.Sections.Select(MapSection))
            {
                Sections.Add(section);
            }

            SelectedSection = selectedSectionId is null
                ? Sections.FirstOrDefault()
                : Sections.FirstOrDefault(section => section.Id == selectedSectionId) ?? Sections.FirstOrDefault();

            RefreshFilteredTestCases(selectedTestCaseId);

            SelectedStep = selectedStepId is null
                ? SelectedTestCase?.Steps.FirstOrDefault()
                : SelectedTestCase?.Steps.FirstOrDefault(step => step.Id == selectedStepId)
                    ?? SelectedTestCase?.Steps.FirstOrDefault();

            UpdateResultSummary();
        }
        catch (Exception ex)
        {
            SelectedSection = null;
            ResultSummary = "No session selected.";
            StatusMessage = ex.Message;
        }
    }

    private void RefreshFilteredTestCases(Guid? preferredTestCaseId = null)
    {
        var selectedStatus = SelectedResultStatusFilter?.Value;
        var testCases = SelectedSection?.TestCases ?? [];
        var filteredTestCases = selectedStatus is null
            ? testCases
            : testCases.Where(testCase => testCase.Status == selectedStatus.Value);

        FilteredTestCases.Clear();
        foreach (var testCase in filteredTestCases)
        {
            FilteredTestCases.Add(testCase);
        }

        SelectedTestCase = preferredTestCaseId is null
            ? FilteredTestCases.FirstOrDefault()
            : FilteredTestCases.FirstOrDefault(testCase => testCase.Id == preferredTestCaseId)
                ?? FilteredTestCases.FirstOrDefault();
    }

    private void UpdateResultSummary()
    {
        var testCases = Sections.SelectMany(section => section.TestCases).ToList();
        if (testCases.Count == 0)
        {
            ResultSummary = "No test cases in selected session.";
            return;
        }

        var passed = testCases.Count(testCase => testCase.Status == TestResultStatus.Pass);
        var failed = testCases.Count(testCase => testCase.Status == TestResultStatus.Fail);
        var blocked = testCases.Count(testCase => testCase.Status == TestResultStatus.Blocked);
        var notTested = testCases.Count(testCase => testCase.Status == TestResultStatus.NotTested);
        var notApplicable = testCases.Count(testCase => testCase.Status == TestResultStatus.NotApplicable);

        ResultSummary = $"{passed} passed, {failed} failed, {blocked} blocked, {notTested} not tested, {notApplicable} N/A";
    }

    private void LoadResultAttachments()
    {
        ResultAttachments.Clear();

        if (!CanAddAttachment())
        {
            return;
        }

        foreach (var attachment in attachmentService
                     .GetAttachments(GetEditingEntityType(), EditingResultId)
                     .Select(MapAttachment))
        {
            ResultAttachments.Add(attachment);
        }
    }

    private EntityReferenceType GetEditingEntityType()
    {
        return EditingResultTarget switch
        {
            TestSessionResultTargetKind.TestCase => EntityReferenceType.TestCaseResult,
            TestSessionResultTargetKind.Step => EntityReferenceType.TestStepResult,
            _ => throw new InvalidOperationException("No result item is selected for attachments.")
        };
    }

    private static TestSessionSummaryViewModel MapSession(TestSessionSummaryItem session)
    {
        return new TestSessionSummaryViewModel(
            session.Id,
            session.Name,
            session.TestSuiteName,
            session.TestSuiteRevisionName,
            session.TestedVersion,
            session.BuildNumber,
            session.CreatedBy,
            session.CreatedAt,
            session.SectionCount,
            session.TestCaseCount,
            session.StepCount);
    }

    private static TestSectionResultViewModel MapSection(TestSectionResultItem section)
    {
        return new TestSectionResultViewModel(
            section.Id,
            section.Name,
            section.Category,
            section.SortOrder,
            section.TestCases
                .OrderBy(testCase => testCase.SortOrder)
                .Select(MapTestCase));
    }

    private static TestCaseResultViewModel MapTestCase(TestCaseResultItem testCase)
    {
        return new TestCaseResultViewModel(
            testCase.Id,
            testCase.Title,
            testCase.ExpectedResult,
            testCase.SortOrder,
            testCase.Status,
            testCase.Comment,
            testCase.Steps
                .OrderBy(step => step.SortOrder)
                .Select(MapStep));
    }

    private static TestStepResultViewModel MapStep(TestStepResultItem step)
    {
        return new TestStepResultViewModel(
            step.Id,
            step.StepText,
            step.ExpectedResult,
            step.SortOrder,
            step.Status,
            step.Comment);
    }

    private static AttachmentItemViewModel MapAttachment(AttachmentItem attachment)
    {
        return new AttachmentItemViewModel(
            attachment.Id,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedBy,
            attachment.UploadedAt);
    }
}
