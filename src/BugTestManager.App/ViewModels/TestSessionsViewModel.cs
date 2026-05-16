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
    private static readonly Guid CopyPreviousSessionTemplateOptionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly ITestSessionService testSessionService;
    private readonly ITestSessionTemplateSyncService templateSyncService;
    private readonly ITestSuiteCatalogService testSuiteCatalogService;
    private readonly IAttachmentService attachmentService;
    private readonly ICustomFieldValueService customFieldValueService;
    private readonly IDiscussionService discussionService;
    private readonly IBugReportService bugReportService;
    private readonly IFilePickerService filePickerService;
    private readonly IFileLauncherService fileLauncherService;
    private readonly IErrorDialogService errorDialogService;
    private readonly INotificationDialogService notificationDialogService;
    private readonly IProjectContext projectContext;
    private readonly IUserContext userContext;

    public TestSessionsViewModel(
        ITestSessionService testSessionService,
        ITestSessionTemplateSyncService templateSyncService,
        ITestSuiteCatalogService testSuiteCatalogService,
        IAttachmentService attachmentService,
        ICustomFieldValueService customFieldValueService,
        IDiscussionService discussionService,
        IBugReportService bugReportService,
        IFilePickerService filePickerService,
        IFileLauncherService fileLauncherService,
        IErrorDialogService errorDialogService,
        INotificationDialogService notificationDialogService,
        IProjectContext projectContext,
        IUserContext userContext)
    {
        this.testSessionService = testSessionService;
        this.templateSyncService = templateSyncService;
        this.testSuiteCatalogService = testSuiteCatalogService;
        this.attachmentService = attachmentService;
        this.customFieldValueService = customFieldValueService;
        this.discussionService = discussionService;
        this.bugReportService = bugReportService;
        this.filePickerService = filePickerService;
        this.fileLauncherService = fileLauncherService;
        this.errorDialogService = errorDialogService;
        this.notificationDialogService = notificationDialogService;
        this.projectContext = projectContext;
        this.userContext = userContext;
        TestSuites = [];
        Revisions = [];
        Sessions = [];
        CopySourceSessions = [];
        Sections = [];
        FilteredTestCases = [];
        ResultAttachments = [];
        ResultCustomFields = [];
        ResultLinkedBugs = [];
        DiscussionComments = [];
        ResultLinkedBugs.CollectionChanged += (_, _) => NotifyResultLinkedBugPropertiesChanged();
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

    public ObservableCollection<TestSessionSummaryViewModel> CopySourceSessions { get; }

    public ObservableCollection<TestSectionResultViewModel> Sections { get; }

    public ObservableCollection<TestCaseResultViewModel> FilteredTestCases { get; }

    public ObservableCollection<AttachmentItemViewModel> ResultAttachments { get; }

    public ObservableCollection<CustomFieldValueItemViewModel> ResultCustomFields { get; }

    public ObservableCollection<LinkedBugSummaryViewModel> ResultLinkedBugs { get; }

    public ObservableCollection<DiscussionCommentItemViewModel> DiscussionComments { get; }

    public IReadOnlyList<SelectionOption<TestResultStatus>> ResultStatuses { get; }

    public IReadOnlyList<SelectionOption<TestResultStatus?>> ResultStatusFilters { get; }

    public Visibility RevisionPickerVisibility => SelectedTestSuite?.RevisionIsRequired == true
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility PreviousSessionPickerVisibility => IsCopyPreviousSessionTemplateOption(SelectedTestSuite)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility EmptyStateVisibility => Sessions.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility SessionWorkspaceVisibility => Sessions.Count == 0
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility ResultLinkedBugListVisibility => ResultLinkedBugs.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility ResultLinkedBugEmptyVisibility => ResultLinkedBugs.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility ResultLinkedBugBadgeVisibility => ResultLinkedBugs.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string ResultLinkedBugButtonText => ResultLinkedBugs.Count == 0
        ? "Linked Bug"
        : ResultLinkedBugs.Count == 1
            ? "1 Linked Bug"
            : $"{ResultLinkedBugs.Count} Linked Bugs";

    public string CreateLinkedBugButtonText => ResultLinkedBugs.Count == 0
        ? "Create Bug"
        : "Create Another Bug";

    public Visibility TemplateSyncUpdateVisibility => TemplateSyncPreview?.CanUpdateOriginalTemplate == true
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility TemplateSyncCreateOnlyVisibility => TemplateSyncPreview?.CanUpdateOriginalTemplate == true
        ? Visibility.Collapsed
        : Visibility.Visible;

    public string TemplateSyncOriginalDisplay => TemplateSyncPreview is null
        ? "No session selected."
        : TemplateSyncPreview.CanUpdateOriginalTemplate
            ? $"{TemplateSyncPreview.OriginalTemplateName}{BuildRevisionSuffix(TemplateSyncPreview.OriginalRevisionName)}"
            : "This session was created without an original template.";

    public string TemplateSyncTotalsDisplay => TemplateSyncPreview is null
        ? string.Empty
        : $"{TemplateSyncPreview.TotalSections} sections, {TemplateSyncPreview.TotalTestCases} cases, {TemplateSyncPreview.TotalChecks} checks in this session.";

    public string TemplateSyncNewItemsDisplay => TemplateSyncPreview is null
        ? string.Empty
        : $"{TemplateSyncPreview.NewSectionCount} new sections, {TemplateSyncPreview.NewTestCaseCount} new cases, {TemplateSyncPreview.NewCheckCount} new checks can be appended to the original template.";

    public string TemplateSyncUpdateButtonText => TemplateSyncPreview is null
        ? "Update Existing Template"
        : TemplateSyncPreview.NewSectionCount == 0
            && TemplateSyncPreview.NewTestCaseCount == 0
            && TemplateSyncPreview.NewCheckCount == 0
                ? "No New Structure"
                : "Update Existing Template";

    [ObservableProperty]
    private TestSessionSuiteOption? selectedTestSuite;

    [ObservableProperty]
    private TestSessionRevisionOption? selectedRevision;

    [ObservableProperty]
    private TestSessionSummaryViewModel? selectedSession;

    [ObservableProperty]
    private TestSessionSummaryViewModel? copySourceSession;

    public GridLength ResultDetailsColumnWidth => ResultDialogOverlayVisibility == Visibility.Visible
        ? new(420)
        : new(0);

    public GridLength ResultDetailsGapWidth => ResultDialogOverlayVisibility == Visibility.Visible
        ? new(16)
        : new(0);

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
    private Visibility newSessionDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility deleteSessionDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string deleteSessionTitle = string.Empty;

    [ObservableProperty]
    private string deleteSessionWarning = string.Empty;

    private Guid pendingDeleteSessionId;

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
    private Visibility linkedBugDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility templateSyncDialogVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private TestSessionTemplateSyncPreview? templateSyncPreview;

    [ObservableProperty]
    private string templateSyncNewTemplateName = string.Empty;

    [ObservableProperty]
    private string templateSyncDescription = string.Empty;

    [ObservableProperty]
    private string templateSyncStatusMessage = string.Empty;

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
        if (IsCopyPreviousSessionTemplateOption(value) && CopySourceSession is null)
        {
            CopySourceSession = SelectedSession ?? CopySourceSessions.FirstOrDefault();
        }

        OnPropertyChanged(nameof(RevisionPickerVisibility));
        OnPropertyChanged(nameof(PreviousSessionPickerVisibility));
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRevisionChanged(TestSessionRevisionOption? value)
    {
        CreateSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSessionChanged(TestSessionSummaryViewModel? value)
    {
        if (CopySourceSession is null && value is not null)
        {
            CopySourceSession = value;
        }

        LoadSelectedSession(value?.Id);
        ShowCreateManualSectionDialogCommand.NotifyCanExecuteChanged();
        ShowDeleteSessionDialogCommand.NotifyCanExecuteChanged();
        ShowTemplateSyncDialogCommand.NotifyCanExecuteChanged();
    }

    partial void OnCopySourceSessionChanged(TestSessionSummaryViewModel? value)
    {
        CreateSessionCommand.NotifyCanExecuteChanged();
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

        OnPropertyChanged(nameof(ResultDetailsColumnWidth));
        OnPropertyChanged(nameof(ResultDetailsGapWidth));
        SaveResultCommand.NotifyCanExecuteChanged();
        AddAttachmentCommand.NotifyCanExecuteChanged();
        CreateLinkedBugCommand.NotifyCanExecuteChanged();
        ShowLinkedBugDialogCommand.NotifyCanExecuteChanged();
        ShowCurrentResultDiscussionCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditingResultIdChanged(Guid value)
    {
        SaveResultCommand.NotifyCanExecuteChanged();
        AddAttachmentCommand.NotifyCanExecuteChanged();
        CreateLinkedBugCommand.NotifyCanExecuteChanged();
        ShowLinkedBugDialogCommand.NotifyCanExecuteChanged();
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

    partial void OnTemplateSyncPreviewChanged(TestSessionTemplateSyncPreview? value)
    {
        OnPropertyChanged(nameof(TemplateSyncUpdateVisibility));
        OnPropertyChanged(nameof(TemplateSyncCreateOnlyVisibility));
        OnPropertyChanged(nameof(TemplateSyncOriginalDisplay));
        OnPropertyChanged(nameof(TemplateSyncTotalsDisplay));
        OnPropertyChanged(nameof(TemplateSyncNewItemsDisplay));
        OnPropertyChanged(nameof(TemplateSyncUpdateButtonText));
        UpdateOriginalTemplateFromSessionCommand.NotifyCanExecuteChanged();
        CreateTemplateFromSessionCommand.NotifyCanExecuteChanged();
    }

    partial void OnTemplateSyncNewTemplateNameChanged(string value)
    {
        CreateTemplateFromSessionCommand.NotifyCanExecuteChanged();
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

    [RelayCommand]
    private void ShowNewSessionDialog()
    {
        StatusMessage = "Ready";
        CopySourceSession ??= SelectedSession ?? CopySourceSessions.FirstOrDefault();
        NewSessionDialogVisibility = Visibility.Visible;
    }

    [RelayCommand]
    private void CloseNewSessionDialog()
    {
        NewSessionDialogVisibility = Visibility.Collapsed;
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
            Guid sessionId;
            if (IsCopyPreviousSessionTemplateOption(SelectedTestSuite))
            {
                if (CopySourceSession is null)
                {
                    return;
                }

                sessionId = testSessionService.CopySession(new CopyTestSessionRequest(
                    NewSessionName,
                    CopySourceSession.Id,
                    TestedVersion,
                    BuildNumber,
                    Notes,
                    userContext.UserName,
                    projectContext.CurrentProjectId));
            }
            else if (CreateSessionWithoutTemplate)
            {
                sessionId = testSessionService.CreateManualSession(new CreateManualTestSessionRequest(
                    NewSessionName,
                    TestedVersion,
                    BuildNumber,
                    Notes,
                    userContext.UserName,
                    projectContext.CurrentProjectId));
            }
            else
            {
                sessionId = testSessionService.CreateSession(new CreateTestSessionRequest(
                    NewSessionName,
                    SelectedTestSuite!.Id,
                    SelectedRevision?.Id,
                    TestedVersion,
                    BuildNumber,
                    Notes,
                    userContext.UserName,
                    projectContext.CurrentProjectId));
            }

            NewSessionName = string.Empty;
            TestedVersion = string.Empty;
            BuildNumber = string.Empty;
            Notes = string.Empty;

            LoadSessions(sessionId);
            StatusMessage = IsCopyPreviousSessionTemplateOption(SelectedTestSuite)
                ? "Test session copied from previous session."
                : CreateSessionWithoutTemplate
                    ? "Manual test session created."
                    : "Test session created from template.";
            NewSessionDialogVisibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowDeleteSessionDialog))]
    private void ShowDeleteSessionDialog(TestSessionSummaryViewModel? session)
    {
        var targetSession = session ?? SelectedSession;
        if (targetSession is null)
        {
            return;
        }

        SelectedSession = targetSession;
        pendingDeleteSessionId = targetSession.Id;
        DeleteSessionTitle = $"Delete session: {targetSession.Name}";
        DeleteSessionWarning = "This will permanently delete this test session with its results, attachments, custom field values, and discussions.";
        StatusMessage = "This action cannot be undone.";
        DeleteSessionDialogVisibility = Visibility.Visible;
        ConfirmDeleteSessionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CloseDeleteSessionDialog()
    {
        DeleteSessionDialogVisibility = Visibility.Collapsed;
        pendingDeleteSessionId = Guid.Empty;
        DeleteSessionTitle = string.Empty;
        DeleteSessionWarning = string.Empty;
        ConfirmDeleteSessionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmDeleteSession))]
    private void ConfirmDeleteSession()
    {
        if (pendingDeleteSessionId == Guid.Empty)
        {
            return;
        }

        try
        {
            var deletedSessionId = pendingDeleteSessionId;
            testSessionService.DeleteSession(deletedSessionId);
            CloseDeleteSessionDialog();
            LoadSessions(Sessions.FirstOrDefault(session => session.Id != deletedSessionId)?.Id);
            StatusMessage = "Test session deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowTemplateSyncDialog))]
    private void ShowTemplateSyncDialog()
    {
        if (SelectedSession is null)
        {
            return;
        }

        try
        {
            TemplateSyncPreview = templateSyncService.GetPreview(
                SelectedSession.Id,
                projectContext.CurrentProjectId);
            TemplateSyncNewTemplateName = BuildDefaultTemplateName(SelectedSession);
            TemplateSyncDescription = $"Created from test session '{SelectedSession.Name}'.";
            TemplateSyncStatusMessage = "Review the template update before saving changes.";
            TemplateSyncDialogVisibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Template Sync Error", ex.Message);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void CloseTemplateSyncDialog()
    {
        TemplateSyncDialogVisibility = Visibility.Collapsed;
        TemplateSyncPreview = null;
        TemplateSyncNewTemplateName = string.Empty;
        TemplateSyncDescription = string.Empty;
        TemplateSyncStatusMessage = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanUpdateOriginalTemplateFromSession))]
    private void UpdateOriginalTemplateFromSession()
    {
        if (SelectedSession is null)
        {
            return;
        }

        try
        {
            var result = templateSyncService.UpdateOriginalTemplate(new UpdateTemplateFromSessionRequest(
                SelectedSession.Id,
                projectContext.CurrentProjectId));

            var selectedSessionId = SelectedSession.Id;
            LoadTestSuites();
            LoadSessions(selectedSessionId);
            CloseTemplateSyncDialog();
            var message = $"Template '{result.TestSuiteName}' was updated successfully. Added {result.AddedSections} sections, {result.AddedTestCases} cases, and {result.AddedChecks} checks.";
            StatusMessage = message;
            notificationDialogService.ShowInfo("Template Saved", message);
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Template Sync Error", ex.Message);
            TemplateSyncStatusMessage = ex.Message;
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateTemplateFromSession))]
    private void CreateTemplateFromSession()
    {
        if (SelectedSession is null)
        {
            return;
        }

        try
        {
            var result = templateSyncService.CreateTemplateFromSession(new CreateTemplateFromSessionRequest(
                SelectedSession.Id,
                TemplateSyncNewTemplateName,
                TemplateSyncDescription,
                projectContext.CurrentProjectId));

            LoadTestSuites();
            CloseTemplateSyncDialog();
            var message = $"Template '{result.TestSuiteName}' was created successfully with {result.AddedSections} sections, {result.AddedTestCases} cases, and {result.AddedChecks} checks.";
            StatusMessage = message;
            notificationDialogService.ShowInfo("Template Created", message);
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Template Sync Error", ex.Message);
            TemplateSyncStatusMessage = ex.Message;
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
        LoadResultLinkedBugs();
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
        LoadResultLinkedBugs();
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
        ResultLinkedBugs.Clear();
        LinkedBugDialogVisibility = Visibility.Collapsed;
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
                LinkedBugTargetDisplay,
                projectContext.CurrentProjectId));

            var sessionId = SelectedSession?.Id;
            var sectionId = SelectedSection?.Id;
            var testCaseId = EditingResultTarget == TestSessionResultTargetKind.TestCase
                ? EditingResultId
                : SelectedTestCase?.Id;
            var stepId = EditingResultTarget == TestSessionResultTargetKind.Step
                ? EditingResultId
                : SelectedStep?.Id;

            LoadSelectedSession(sessionId, sectionId, testCaseId, stepId);
            LoadResultLinkedBugs();
            StatusMessage = "Linked bug created.";
            LinkedBugDialogVisibility = Visibility.Collapsed;
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

    [RelayCommand(CanExecute = nameof(CanShowLinkedBugDialog))]
    private void ShowLinkedBugDialog()
    {
        LinkedBugDialogVisibility = Visibility.Visible;
    }

    [RelayCommand]
    private void CloseLinkedBugDialog()
    {
        LinkedBugDialogVisibility = Visibility.Collapsed;
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
            discussionService.MarkRead(discussionEntityType, discussionEntityId, userContext.UserName);
            LoadDiscussionComments();
            RefreshResultDiscussionBadges();
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
            RefreshResultDiscussionBadges();
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
            && (IsCopyPreviousSessionTemplateOption(SelectedTestSuite)
                ? CopySourceSession is not null
                : CreateSessionWithoutTemplate
                || (SelectedTestSuite is not null
                    && (!SelectedTestSuite.RevisionIsRequired || SelectedRevision?.Id is not null)));
    }

    private bool CanShowDeleteSessionDialog(TestSessionSummaryViewModel? session)
    {
        return session is not null || SelectedSession is not null;
    }

    private bool CanConfirmDeleteSession()
    {
        return pendingDeleteSessionId != Guid.Empty;
    }

    private bool CanShowTemplateSyncDialog()
    {
        return SelectedSession is not null;
    }

    private bool CanUpdateOriginalTemplateFromSession()
    {
        return TemplateSyncPreview?.CanUpdateOriginalTemplate == true
            && (TemplateSyncPreview.NewSectionCount > 0
                || TemplateSyncPreview.NewTestCaseCount > 0
                || TemplateSyncPreview.NewCheckCount > 0);
    }

    private bool CanCreateTemplateFromSession()
    {
        return TemplateSyncPreview is not null
            && !string.IsNullOrWhiteSpace(TemplateSyncNewTemplateName);
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

    private bool CanShowLinkedBugDialog()
    {
        return CanAddAttachment();
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
                [new TestSessionRevisionOption(null, "No revision")]),
            new(
                CopyPreviousSessionTemplateOptionId,
                "Copy previous session",
                revisionIsRequired: false,
                [new TestSessionRevisionOption(null, "No revision")])
        };

        suites.AddRange(testSuiteCatalogService.GetCatalog(projectContext.CurrentProjectId)
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
            ? TestSuites.FirstOrDefault(suite =>
                !IsManualSessionTemplateOption(suite)
                && !IsCopyPreviousSessionTemplateOption(suite)) ?? TestSuites.FirstOrDefault()
            : TestSuites.FirstOrDefault(suite => suite.Id == selectedSuiteId) ?? TestSuites.FirstOrDefault();
    }

    private static bool IsManualSessionTemplateOption(TestSessionSuiteOption? option)
    {
        return option?.Id == ManualSessionTemplateOptionId;
    }

    private static bool IsCopyPreviousSessionTemplateOption(TestSessionSuiteOption? option)
    {
        return option?.Id == CopyPreviousSessionTemplateOptionId;
    }

    private void LoadSessions(Guid? selectedSessionId = null)
    {
        var preferredSessionId = selectedSessionId ?? SelectedSession?.Id;
        var preferredCopySourceSessionId = CopySourceSession?.Id;
        var sessions = testSessionService.GetSessions(projectContext.CurrentProjectId)
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

        CopySourceSessions.Clear();
        foreach (var session in sessions)
        {
            CopySourceSessions.Add(session);
        }

        CopySourceSession = preferredCopySourceSessionId is null
            ? SelectedSession ?? CopySourceSessions.FirstOrDefault()
            : CopySourceSessions.FirstOrDefault(session => session.Id == preferredCopySourceSessionId)
                ?? SelectedSession
                ?? CopySourceSessions.FirstOrDefault();

        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(SessionWorkspaceVisibility));
        CreateSessionCommand.NotifyCanExecuteChanged();
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

            RefreshResultDiscussionBadges();
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
                     .GetValues(GetEditingEntityType(), EditingResultId, BuildCurrentResultScopes(), projectContext.CurrentProjectId)
                     .Select(MapCustomField))
        {
            ResultCustomFields.Add(field);
        }
    }

    private void LoadResultLinkedBugs()
    {
        ResultLinkedBugs.Clear();

        IEnumerable<LinkedBugSummaryViewModel> linkedBugs = EditingResultTarget switch
        {
            TestSessionResultTargetKind.TestCase when SelectedTestCase is not null => SelectedTestCase.LinkedBugs,
            TestSessionResultTargetKind.Step when SelectedStep is not null => SelectedStep.LinkedBugs,
            _ => []
        };

        foreach (var linkedBug in linkedBugs)
        {
            ResultLinkedBugs.Add(linkedBug);
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
        discussionService.MarkRead(entityType, entityId, userContext.UserName);
        RefreshResultDiscussionBadges();
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

    private void RefreshResultDiscussionBadges()
    {
        foreach (var testCase in Sections.SelectMany(section => section.TestCases))
        {
            testCase.UnreadDiscussionCount = discussionService.GetUnreadCount(
                EntityReferenceType.TestCaseResult,
                testCase.Id,
                userContext.UserName);

            foreach (var step in testCase.Steps)
            {
                step.UnreadDiscussionCount = discussionService.GetUnreadCount(
                    EntityReferenceType.TestStepResult,
                    step.Id,
                    userContext.UserName);
            }
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
            testCase.LinkedBugs.Select(MapLinkedBug),
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
            step.Comment,
            step.LinkedBugs.Select(MapLinkedBug));
    }

    private static LinkedBugSummaryViewModel MapLinkedBug(LinkedBugSummaryItem bug)
    {
        return new LinkedBugSummaryViewModel(
            bug.Id,
            bug.Title,
            bug.Status);
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

    private static string BuildDefaultTemplateName(TestSessionSummaryViewModel session)
    {
        return $"{session.Name} Template";
    }

    private static string BuildRevisionSuffix(string? revisionName)
    {
        return string.IsNullOrWhiteSpace(revisionName)
            ? string.Empty
            : $" / {revisionName}";
    }

    private void NotifyResultLinkedBugPropertiesChanged()
    {
        OnPropertyChanged(nameof(ResultLinkedBugListVisibility));
        OnPropertyChanged(nameof(ResultLinkedBugEmptyVisibility));
        OnPropertyChanged(nameof(ResultLinkedBugBadgeVisibility));
        OnPropertyChanged(nameof(ResultLinkedBugButtonText));
        OnPropertyChanged(nameof(CreateLinkedBugButtonText));
    }
}
