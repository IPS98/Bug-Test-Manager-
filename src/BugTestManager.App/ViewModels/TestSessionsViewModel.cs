using System.Collections.ObjectModel;
using System.Windows;
using BugTestManager.App.Services;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Exceptions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BugTestManager.App.ViewModels;

public sealed partial class TestSessionsViewModel : ObservableObject
{
    private static readonly Guid ManualSessionTemplateOptionId = Guid.Empty;

    private readonly ITestSessionService testSessionService;
    private readonly ITestSuiteCatalogService testSuiteCatalogService;
    private readonly IAttachmentService attachmentService;
    private readonly ICustomFieldValueService customFieldValueService;
    private readonly IDiscussionService discussionService;
    private readonly IBugReportService bugReportService;
    private readonly IFilePickerService filePickerService;
    private readonly IFileLauncherService fileLauncherService;
    private readonly IErrorDialogService errorDialogService;
    private readonly IUserContext userContext;

    public TestSessionsViewModel(
        ITestSessionService testSessionService,
        ITestSuiteCatalogService testSuiteCatalogService,
        IAttachmentService attachmentService,
        ICustomFieldValueService customFieldValueService,
        IDiscussionService discussionService,
        IBugReportService bugReportService,
        IFilePickerService filePickerService,
        IFileLauncherService fileLauncherService,
        IErrorDialogService errorDialogService,
        IUserContext userContext)
    {
        this.testSessionService = testSessionService;
        this.testSuiteCatalogService = testSuiteCatalogService;
        this.attachmentService = attachmentService;
        this.customFieldValueService = customFieldValueService;
        this.discussionService = discussionService;
        this.bugReportService = bugReportService;
        this.filePickerService = filePickerService;
        this.fileLauncherService = fileLauncherService;
        this.errorDialogService = errorDialogService;
        this.userContext = userContext;
        TestSuites = [];
        Revisions = [];
        Sessions = [];
        Sections = [];
        FilteredTestCases = [];
        ResultAttachments = [];
        ResultCustomFields = [];
        DiscussionComments = [];
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

    public ObservableCollection<CustomFieldValueItemViewModel> ResultCustomFields { get; }

    public ObservableCollection<DiscussionCommentItemViewModel> DiscussionComments { get; }

    public IReadOnlyList<SelectionOption<TestResultStatus>> ResultStatuses { get; }

    public IReadOnlyList<SelectionOption<TestResultStatus?>> ResultStatusFilters { get; }

    public Visibility RevisionPickerVisibility => SelectedTestSuite?.RevisionIsRequired == true
        ? Visibility.Visible
        : Visibility.Collapsed;

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
    private bool createSessionWithoutTemplate;

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

    [ObservableProperty]
    private string linkedBugTitle = string.Empty;

    [ObservableProperty]
    private string linkedBugDescription = string.Empty;

    [ObservableProperty]
    private string linkedBugTargetDisplay = string.Empty;

    [ObservableProperty]
    private Visibility discussionDrawerVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string discussionTitle = "Discussion";

    [ObservableProperty]
    private string discussionMessage = string.Empty;

    [ObservableProperty]
    private DiscussionCommentItemViewModel? editingDiscussionComment;

    [ObservableProperty]
    private string discussionEditorTitle = "New message";

    [ObservableProperty]
    private string discussionSaveButtonText = "Add Message";

    [ObservableProperty]
    private ManualTestSessionDialogKind manualDialogKind;

    [ObservableProperty]
    private Visibility manualDialogOverlayVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string manualDialogTitle = string.Empty;

    [ObservableProperty]
    private string manualItemName = string.Empty;

    [ObservableProperty]
    private string manualItemDescription = string.Empty;

    private EntityReferenceType discussionEntityType;

    private Guid discussionEntityId;

    partial void OnSelectedTestSuiteChanged(TestSessionSuiteOption? value)
    {
        CreateSessionWithoutTemplate = IsManualSessionTemplateOption(value);
        Revisions.Clear();

        if (value is not null)
        {
            foreach (var revision in value.Revisions)
            {
                Revisions.Add(revision);
            }
        }

        SelectedRevision = Revisions.FirstOrDefault();
        OnPropertyChanged(nameof(RevisionPickerVisibility));
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRevisionChanged(TestSessionRevisionOption? value)
    {
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSessionChanged(TestSessionSummaryViewModel? value)
    {
        LoadSelectedSession(value?.Id);
        ShowCreateManualSectionDialogCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSectionChanged(TestSectionResultViewModel? value)
    {
        RefreshFilteredTestCases();
        ShowCreateManualTestCaseDialogCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTestCaseChanged(TestCaseResultViewModel? value)
    {
        SelectedStep = value?.Steps.FirstOrDefault();
        ShowEditTestCaseResultCommand.NotifyCanExecuteChanged();
        ShowTestCaseDiscussionCommand.NotifyCanExecuteChanged();
        ShowCreateManualCheckDialogCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedStepChanged(TestStepResultViewModel? value)
    {
        ShowEditTestStepResultCommand.NotifyCanExecuteChanged();
        ShowTestStepDiscussionCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewSessionNameChanged(string value)
    {
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnCreateSessionWithoutTemplateChanged(bool value)
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
        CreateLinkedBugCommand.NotifyCanExecuteChanged();
        ShowCurrentResultDiscussionCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditingResultIdChanged(Guid value)
    {
        SaveResultCommand.NotifyCanExecuteChanged();
        AddAttachmentCommand.NotifyCanExecuteChanged();
        CreateLinkedBugCommand.NotifyCanExecuteChanged();
        ShowCurrentResultDiscussionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedResultStatusChanged(SelectionOption<TestResultStatus>? value)
    {
        SaveResultCommand.NotifyCanExecuteChanged();
    }

    partial void OnLinkedBugTitleChanged(string value)
    {
        CreateLinkedBugCommand.NotifyCanExecuteChanged();
    }

    partial void OnDiscussionMessageChanged(string value)
    {
        SaveDiscussionMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedResultStatusFilterChanged(SelectionOption<TestResultStatus?>? value)
    {
        RefreshFilteredTestCases(SelectedTestCase?.Id);
    }

    partial void OnManualDialogKindChanged(ManualTestSessionDialogKind value)
    {
        ManualDialogOverlayVisibility = value == ManualTestSessionDialogKind.None
            ? Visibility.Collapsed
            : Visibility.Visible;

        ManualDialogTitle = value switch
        {
            ManualTestSessionDialogKind.Section => "Add Manual Section",
            ManualTestSessionDialogKind.TestCase => "Add Manual Test Case",
            ManualTestSessionDialogKind.Check => "Add Manual Check",
            _ => string.Empty
        };

        SaveManualItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnManualItemNameChanged(string value)
    {
        SaveManualItemCommand.NotifyCanExecuteChanged();
    }

    public void Refresh()
    {
        LoadTestSuites();
        LoadSessions(SelectedSession?.Id);
    }

    [RelayCommand(CanExecute = nameof(CanCreateSession))]
    private void CreateSession()
    {
        if (!CreateSessionWithoutTemplate && SelectedTestSuite is null)
        {
            return;
        }

        try
        {
            var sessionId = CreateSessionWithoutTemplate
                ? testSessionService.CreateManualSession(new CreateManualTestSessionRequest(
                    NewSessionName,
                    TestedVersion,
                    BuildNumber,
                    Notes,
                    userContext.UserName))
                : testSessionService.CreateSession(new CreateTestSessionRequest(
                    NewSessionName,
                    SelectedTestSuite!.Id,
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
            StatusMessage = CreateSessionWithoutTemplate
                ? "Manual test session created."
                : "Test session created from template.";
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
        LinkedBugTitle = testCase.Title;
        LinkedBugDescription = BuildDefaultBugDescription(testCase.ExpectedResult, testCase.Comment);
        LinkedBugTargetDisplay = $"Test case: {testCase.Title}";
        ResultDialogTitle = $"Update Test Case: {testCase.Title}";
        StatusMessage = "Ready";
        EditingResultTarget = TestSessionResultTargetKind.TestCase;
        LoadResultAttachments();
        LoadResultCustomFields();
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
        LinkedBugTitle = step.CheckText;
        LinkedBugDescription = BuildDefaultBugDescription(step.ExpectedResult, step.Comment);
        LinkedBugTargetDisplay = $"Check {step.SortOrder}: {step.CheckText}";
        ResultDialogTitle = $"Update Check {step.SortOrder}";
        StatusMessage = "Ready";
        EditingResultTarget = TestSessionResultTargetKind.Step;
        LoadResultAttachments();
        LoadResultCustomFields();
    }

    [RelayCommand]
    private void CloseResultDialog()
    {
        EditingResultTarget = TestSessionResultTargetKind.None;
        EditingResultId = Guid.Empty;
        ResultDialogTitle = string.Empty;
        ResultComment = string.Empty;
        LinkedBugTitle = string.Empty;
        LinkedBugDescription = string.Empty;
        LinkedBugTargetDisplay = string.Empty;
        SelectedResultStatus = ResultStatuses.FirstOrDefault();
        ResultAttachments.Clear();
        ResultCustomFields.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateManualSectionDialog))]
    private void ShowCreateManualSectionDialog()
    {
        ManualItemName = string.Empty;
        ManualItemDescription = string.Empty;
        ManualDialogKind = ManualTestSessionDialogKind.Section;
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateManualTestCaseDialog))]
    private void ShowCreateManualTestCaseDialog()
    {
        ManualItemName = string.Empty;
        ManualItemDescription = string.Empty;
        ManualDialogKind = ManualTestSessionDialogKind.TestCase;
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateManualCheckDialog))]
    private void ShowCreateManualCheckDialog(TestCaseResultViewModel? testCase)
    {
        var targetTestCase = testCase ?? SelectedTestCase;
        if (targetTestCase is null)
        {
            return;
        }

        SelectedTestCase = targetTestCase;
        ManualItemName = string.Empty;
        ManualItemDescription = string.Empty;
        ManualDialogKind = ManualTestSessionDialogKind.Check;
    }

    [RelayCommand]
    private void CloseManualItemDialog()
    {
        ManualDialogKind = ManualTestSessionDialogKind.None;
        ManualItemName = string.Empty;
        ManualItemDescription = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSaveManualItem))]
    private void SaveManualItem()
    {
        var sessionId = SelectedSession?.Id;
        var sectionId = SelectedSection?.Id;
        var testCaseId = SelectedTestCase?.Id;

        try
        {
            switch (ManualDialogKind)
            {
                case ManualTestSessionDialogKind.Section when sessionId is not null:
                    sectionId = testSessionService.CreateManualSection(new CreateManualTestSectionRequest(
                        sessionId.Value,
                        ManualItemName,
                        ManualItemDescription));
                    StatusMessage = "Manual section added.";
                    break;
                case ManualTestSessionDialogKind.TestCase when sectionId is not null:
                    testCaseId = testSessionService.CreateManualTestCase(new CreateManualTestCaseRequest(
                        sectionId.Value,
                        ManualItemName,
                        ManualItemDescription));
                    StatusMessage = "Manual test case added.";
                    break;
                case ManualTestSessionDialogKind.Check when testCaseId is not null:
                    var checkId = testSessionService.CreateManualCheck(new CreateManualTestCheckRequest(
                        testCaseId.Value,
                        ManualItemName,
                        ManualItemDescription));
                    CloseManualItemDialog();
                    LoadSessions(sessionId);
                    LoadSelectedSession(sessionId, sectionId, testCaseId, checkId);
                    StatusMessage = "Manual check added.";
                    return;
                default:
                    return;
            }

            CloseManualItemDialog();
            LoadSessions(sessionId);
            LoadSelectedSession(sessionId, sectionId, testCaseId);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
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

    [RelayCommand]
    private void OpenAttachment(AttachmentItemViewModel? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        try
        {
            fileLauncherService.OpenFile(attachment.AbsolutePath);
            StatusMessage = "Attachment opened.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void DeleteAttachment(AttachmentItemViewModel? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        try
        {
            attachmentService.DeleteAttachment(attachment.Id);
            LoadResultAttachments();
            StatusMessage = "Attachment deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateLinkedBug))]
    private void CreateLinkedBug()
    {
        try
        {
            bugReportService.CreateBug(new CreateBugReportRequest(
                LinkedBugTitle,
                LinkedBugDescription,
                "Medium",
                "Medium",
                SelectedSession?.TestedVersion ?? string.Empty,
                SelectedSession?.BuildNumber ?? string.Empty,
                userContext.UserName,
                GetEditingEntityType(),
                EditingResultId,
                LinkedBugTargetDisplay));

            StatusMessage = "Linked bug created.";
        }
        catch (Exception ex)
        {
            var title = ex is DuplicateBugTitleException
                ? "Duplicate Bug"
                : "Bug Creation Error";
            errorDialogService.ShowError(title, ex.Message);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowTestCaseDiscussion))]
    private void ShowTestCaseDiscussion(TestCaseResultViewModel? testCase)
    {
        var targetTestCase = testCase ?? SelectedTestCase;
        if (targetTestCase is null)
        {
            return;
        }

        SelectedTestCase = targetTestCase;
        OpenDiscussion(
            EntityReferenceType.TestCaseResult,
            targetTestCase.Id,
            $"Test case discussion: {targetTestCase.Title}");
    }

    [RelayCommand(CanExecute = nameof(CanShowTestStepDiscussion))]
    private void ShowTestStepDiscussion(TestStepResultViewModel? step)
    {
        var targetStep = step ?? SelectedStep;
        if (targetStep is null)
        {
            return;
        }

        SelectedStep = targetStep;
        OpenDiscussion(
            EntityReferenceType.TestStepResult,
            targetStep.Id,
            $"Check discussion: {targetStep.CheckText}");
    }

    [RelayCommand(CanExecute = nameof(CanShowCurrentResultDiscussion))]
    private void ShowCurrentResultDiscussion()
    {
        switch (EditingResultTarget)
        {
            case TestSessionResultTargetKind.TestCase when SelectedTestCase is not null:
                ShowTestCaseDiscussion(SelectedTestCase);
                break;
            case TestSessionResultTargetKind.Step when SelectedStep is not null:
                ShowTestStepDiscussion(SelectedStep);
                break;
        }
    }

    [RelayCommand]
    private void CloseDiscussion()
    {
        DiscussionDrawerVisibility = Visibility.Collapsed;
        discussionEntityId = Guid.Empty;
        DiscussionTitle = "Discussion";
        DiscussionMessage = string.Empty;
        ClearDiscussionEdit();
        DiscussionComments.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanSaveDiscussionMessage))]
    private void SaveDiscussionMessage()
    {
        try
        {
            if (EditingDiscussionComment is null)
            {
                discussionService.AddComment(new AddDiscussionCommentRequest(
                    discussionEntityType,
                    discussionEntityId,
                    DiscussionMessage,
                    userContext.UserName));
            }
            else
            {
                discussionService.UpdateComment(new UpdateDiscussionCommentRequest(
                    EditingDiscussionComment.Id,
                    DiscussionMessage,
                    userContext.UserName));
            }

            DiscussionMessage = string.Empty;
            ClearDiscussionEdit();
            LoadDiscussionComments();
            StatusMessage = "Discussion saved.";
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Discussion Error", ex.Message);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void EditDiscussionMessage(DiscussionCommentItemViewModel? comment)
    {
        if (comment is null)
        {
            return;
        }

        EditingDiscussionComment = comment;
        DiscussionMessage = comment.Message;
        DiscussionEditorTitle = "Edit message";
        DiscussionSaveButtonText = "Save Message";
        SaveDiscussionMessageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CancelDiscussionEdit()
    {
        DiscussionMessage = string.Empty;
        ClearDiscussionEdit();
    }

    [RelayCommand]
    private void DeleteDiscussionMessage(DiscussionCommentItemViewModel? comment)
    {
        if (comment is null)
        {
            return;
        }

        try
        {
            discussionService.DeleteComment(comment.Id);
            LoadDiscussionComments();
            StatusMessage = "Discussion message deleted.";
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Discussion Error", ex.Message);
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
            SaveResultCustomFields();

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
        return !string.IsNullOrWhiteSpace(NewSessionName)
            && (CreateSessionWithoutTemplate
                || (SelectedTestSuite is not null
                    && (!SelectedTestSuite.RevisionIsRequired || SelectedRevision?.Id is not null)));
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

    private bool CanCreateLinkedBug()
    {
        return CanAddAttachment() && !string.IsNullOrWhiteSpace(LinkedBugTitle);
    }

    private bool CanShowCreateManualSectionDialog()
    {
        return SelectedSession is not null;
    }

    private bool CanShowCreateManualTestCaseDialog()
    {
        return SelectedSection is not null;
    }

    private bool CanShowCreateManualCheckDialog(TestCaseResultViewModel? testCase)
    {
        return testCase is not null || SelectedTestCase is not null;
    }

    private bool CanSaveManualItem()
    {
        return ManualDialogKind != ManualTestSessionDialogKind.None
            && !string.IsNullOrWhiteSpace(ManualItemName);
    }

    private bool CanShowTestCaseDiscussion(TestCaseResultViewModel? testCase)
    {
        return testCase is not null || SelectedTestCase is not null;
    }

    private bool CanShowTestStepDiscussion(TestStepResultViewModel? step)
    {
        return step is not null || SelectedStep is not null;
    }

    private bool CanShowCurrentResultDiscussion()
    {
        return EditingResultTarget != TestSessionResultTargetKind.None && EditingResultId != Guid.Empty;
    }

    private bool CanSaveDiscussionMessage()
    {
        return discussionEntityId != Guid.Empty && !string.IsNullOrWhiteSpace(DiscussionMessage);
    }

    private void LoadTestSuites()
    {
        var selectedSuiteId = SelectedTestSuite?.Id;
        var suites = new List<TestSessionSuiteOption>
        {
            new(
                ManualSessionTemplateOptionId,
                "Start without template",
                revisionIsRequired: false,
                [new TestSessionRevisionOption(null, "No revision")])
        };

        suites.AddRange(testSuiteCatalogService.GetCatalog()
            .Select(suite => new TestSessionSuiteOption(
                suite.Id,
                suite.Name,
                suite.RevisionIsRequired,
                suite.RevisionIsRequired
                    ? suite.Revisions
                        .Select(revision => new TestSessionRevisionOption(revision.Id, revision.Name))
                        .ToList()
                    : [new TestSessionRevisionOption(null, "No revision")]))
            .ToList());

        TestSuites.Clear();
        foreach (var suite in suites)
        {
            TestSuites.Add(suite);
        }

        SelectedTestSuite = selectedSuiteId is null
            ? TestSuites.FirstOrDefault(suite => !IsManualSessionTemplateOption(suite)) ?? TestSuites.FirstOrDefault()
            : TestSuites.FirstOrDefault(suite => suite.Id == selectedSuiteId) ?? TestSuites.FirstOrDefault();
    }

    private static bool IsManualSessionTemplateOption(TestSessionSuiteOption? option)
    {
        return option?.Id == ManualSessionTemplateOptionId;
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

    private void LoadResultCustomFields()
    {
        ResultCustomFields.Clear();

        if (!CanAddAttachment())
        {
            return;
        }

        foreach (var field in customFieldValueService
                     .GetValues(GetEditingEntityType(), EditingResultId, BuildCurrentResultScopes())
                     .Select(MapCustomField))
        {
            ResultCustomFields.Add(field);
        }
    }

    private void SaveResultCustomFields()
    {
        foreach (var field in ResultCustomFields)
        {
            customFieldValueService.SaveValue(new SaveCustomFieldValueRequest(
                field.FieldDefinitionId,
                GetEditingEntityType(),
                EditingResultId,
                field.Value,
                userContext.UserName));
        }
    }

    private IReadOnlyCollection<CustomFieldValueScopeItem> BuildCurrentResultScopes()
    {
        var scopes = new List<CustomFieldValueScopeItem>();

        if (SelectedSession?.TestSuiteId is { } testSuiteId && testSuiteId != Guid.Empty)
        {
            scopes.Add(new CustomFieldValueScopeItem(CustomFieldScopeType.TestSuite, testSuiteId));
        }

        if (SelectedSection?.TemplateSectionId is { } sectionId && sectionId != Guid.Empty)
        {
            scopes.Add(new CustomFieldValueScopeItem(CustomFieldScopeType.TemplateSection, sectionId));
        }

        if (SelectedTestCase?.TestCaseTemplateId is { } testCaseTemplateId && testCaseTemplateId != Guid.Empty)
        {
            scopes.Add(new CustomFieldValueScopeItem(CustomFieldScopeType.TestCaseTemplate, testCaseTemplateId));
        }

        return scopes;
    }

    private void OpenDiscussion(EntityReferenceType entityType, Guid entityId, string title)
    {
        discussionEntityType = entityType;
        discussionEntityId = entityId;
        DiscussionTitle = title;
        DiscussionMessage = string.Empty;
        ClearDiscussionEdit();
        LoadDiscussionComments();
        DiscussionDrawerVisibility = Visibility.Visible;
        SaveDiscussionMessageCommand.NotifyCanExecuteChanged();
    }

    private void LoadDiscussionComments()
    {
        DiscussionComments.Clear();

        if (discussionEntityId == Guid.Empty)
        {
            return;
        }

        foreach (var comment in discussionService
                     .GetComments(discussionEntityType, discussionEntityId)
                     .Select(MapComment))
        {
            DiscussionComments.Add(comment);
        }
    }

    private void ClearDiscussionEdit()
    {
        EditingDiscussionComment = null;
        DiscussionEditorTitle = "New message";
        DiscussionSaveButtonText = "Add Message";
        SaveDiscussionMessageCommand.NotifyCanExecuteChanged();
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
            session.TestSuiteId,
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
            section.TemplateSectionId,
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
            testCase.TestCaseTemplateId,
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
            step.TestStepTemplateId,
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
            attachment.AbsolutePath,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedBy,
            attachment.UploadedAt);
    }

    private static DiscussionCommentItemViewModel MapComment(DiscussionCommentItem comment)
    {
        return new DiscussionCommentItemViewModel(
            comment.Id,
            comment.Message,
            comment.CreatedBy,
            comment.CreatedAt,
            comment.UpdatedBy,
            comment.UpdatedAt);
    }

    private static CustomFieldValueItemViewModel MapCustomField(CustomFieldValueItem field)
    {
        return new CustomFieldValueItemViewModel(
            field.FieldDefinitionId,
            field.EntityType,
            field.EntityId,
            field.Name,
            field.FieldType,
            field.IsRequired,
            field.SortOrder,
            field.Options,
            field.Value);
    }

    private static string BuildDefaultBugDescription(string expectedResult, string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
        {
            return comment;
        }

        return string.IsNullOrWhiteSpace(expectedResult)
            ? "Created from a test result."
            : $"Expected result: {expectedResult}";
    }
}
