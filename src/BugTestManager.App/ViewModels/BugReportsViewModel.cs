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

public sealed partial class BugReportsViewModel : ObservableObject
{
    private const string AllSeveritiesFilter = "All severities";
    private const string AllPrioritiesFilter = "All priorities";

    private readonly IBugReportService bugReportService;
    private readonly IAttachmentService attachmentService;
    private readonly ICustomFieldValueService customFieldValueService;
    private readonly IDiscussionService discussionService;
    private readonly IFilePickerService filePickerService;
    private readonly IFileLauncherService fileLauncherService;
    private readonly IErrorDialogService errorDialogService;
    private readonly IUserContext userContext;

    private EntityReferenceType discussionEntityType;
    private Guid discussionEntityId;
    private List<BugReportItemViewModel> allBugs = [];

    public BugReportsViewModel(
        IBugReportService bugReportService,
        IAttachmentService attachmentService,
        ICustomFieldValueService customFieldValueService,
        IDiscussionService discussionService,
        IFilePickerService filePickerService,
        IFileLauncherService fileLauncherService,
        IErrorDialogService errorDialogService,
        IUserContext userContext)
    {
        this.bugReportService = bugReportService;
        this.attachmentService = attachmentService;
        this.customFieldValueService = customFieldValueService;
        this.discussionService = discussionService;
        this.filePickerService = filePickerService;
        this.fileLauncherService = fileLauncherService;
        this.errorDialogService = errorDialogService;
        this.userContext = userContext;
        Bugs = [];
        BugAttachments = [];
        NewBugCustomFields = [];
        BugCustomFields = [];
        DiscussionComments = [];
        SeverityFilters = [];
        PriorityFilters = [];
        BugStatuses = Enum.GetValues<BugStatus>()
            .Select(status => new SelectionOption<BugStatus>(
                status,
                BugStatusDisplayNames.ForStatus(status)))
            .ToList();
        BugStatusFilters =
        [
            new SelectionOption<BugStatus?>(null, "All statuses"),
            .. Enum.GetValues<BugStatus>()
                .Select(status => new SelectionOption<BugStatus?>(
                    status,
                    BugStatusDisplayNames.ForStatus(status)))
        ];
        SelectedBugStatus = BugStatuses.FirstOrDefault();
        SelectedBugStatusFilter = BugStatusFilters.FirstOrDefault();
        SelectedSeverityFilter = AllSeveritiesFilter;
        SelectedPriorityFilter = AllPrioritiesFilter;
        DetailSelectedBugStatus = BugStatuses.FirstOrDefault();
        Refresh();
    }

    public ObservableCollection<BugReportItemViewModel> Bugs { get; }

    public ObservableCollection<AttachmentItemViewModel> BugAttachments { get; }

    public ObservableCollection<CustomFieldValueItemViewModel> NewBugCustomFields { get; }

    public ObservableCollection<CustomFieldValueItemViewModel> BugCustomFields { get; }

    public ObservableCollection<DiscussionCommentItemViewModel> DiscussionComments { get; }

    public ObservableCollection<string> SeverityFilters { get; }

    public ObservableCollection<string> PriorityFilters { get; }

    public IReadOnlyList<SelectionOption<BugStatus>> BugStatuses { get; }

    public IReadOnlyList<SelectionOption<BugStatus?>> BugStatusFilters { get; }

    [ObservableProperty]
    private BugReportItemViewModel? selectedBug;

    [ObservableProperty]
    private SelectionOption<BugStatus>? selectedBugStatus;

    [ObservableProperty]
    private SelectionOption<BugStatus?>? selectedBugStatusFilter;

    [ObservableProperty]
    private string selectedSeverityFilter = AllSeveritiesFilter;

    [ObservableProperty]
    private string selectedPriorityFilter = AllPrioritiesFilter;

    [ObservableProperty]
    private string newBugTitle = string.Empty;

    [ObservableProperty]
    private string newBugDescription = string.Empty;

    [ObservableProperty]
    private string newBugSeverity = "Medium";

    [ObservableProperty]
    private string newBugPriority = "Medium";

    [ObservableProperty]
    private string newBugFoundInVersion = string.Empty;

    [ObservableProperty]
    private string newBugBuildNumber = string.Empty;

    [ObservableProperty]
    private Visibility bugDetailsDrawerVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string detailBugTitle = string.Empty;

    [ObservableProperty]
    private string detailBugDescription = string.Empty;

    [ObservableProperty]
    private string detailBugSeverity = string.Empty;

    [ObservableProperty]
    private string detailBugPriority = string.Empty;

    [ObservableProperty]
    private string detailBugFoundInVersion = string.Empty;

    [ObservableProperty]
    private string detailBugBuildNumber = string.Empty;

    [ObservableProperty]
    private SelectionOption<BugStatus>? detailSelectedBugStatus;

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
    private string statusMessage = "Ready";

    partial void OnSelectedBugChanged(BugReportItemViewModel? value)
    {
        SelectedBugStatus = value is null
            ? BugStatuses.FirstOrDefault()
            : BugStatuses.Single(option => option.Value == value.Status);
        LoadSelectedBugAttachments();
        UpdateBugStatusCommand.NotifyCanExecuteChanged();
        AddBugAttachmentCommand.NotifyCanExecuteChanged();
        OpenBugDetailsCommand.NotifyCanExecuteChanged();
        OpenBugDiscussionCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewBugTitleChanged(string value)
    {
        CreateBugCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedBugStatusChanged(SelectionOption<BugStatus>? value)
    {
        UpdateBugStatusCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedBugStatusFilterChanged(SelectionOption<BugStatus?>? value)
    {
        ApplyBugFilters(SelectedBug?.Id);
    }

    partial void OnSelectedSeverityFilterChanged(string value)
    {
        ApplyBugFilters(SelectedBug?.Id);
    }

    partial void OnSelectedPriorityFilterChanged(string value)
    {
        ApplyBugFilters(SelectedBug?.Id);
    }

    partial void OnDetailBugTitleChanged(string value)
    {
        SaveBugDetailsCommand.NotifyCanExecuteChanged();
    }

    partial void OnDetailSelectedBugStatusChanged(SelectionOption<BugStatus>? value)
    {
        SaveBugDetailsCommand.NotifyCanExecuteChanged();
    }

    partial void OnDiscussionMessageChanged(string value)
    {
        SaveDiscussionMessageCommand.NotifyCanExecuteChanged();
    }

    public void Refresh()
    {
        LoadNewBugCustomFields();
        LoadBugs(SelectedBug?.Id);
    }

    [RelayCommand(CanExecute = nameof(CanCreateBug))]
    private void CreateBug()
    {
        try
        {
            ValidateRequiredCustomFields(NewBugCustomFields);

            var bugId = bugReportService.CreateBug(new CreateBugReportRequest(
                NewBugTitle,
                NewBugDescription,
                NewBugSeverity,
                NewBugPriority,
                NewBugFoundInVersion,
                NewBugBuildNumber,
                userContext.UserName));

            SaveCustomFields(NewBugCustomFields, bugId);
            NewBugTitle = string.Empty;
            NewBugDescription = string.Empty;
            NewBugSeverity = "Medium";
            NewBugPriority = "Medium";
            NewBugFoundInVersion = string.Empty;
            NewBugBuildNumber = string.Empty;
            LoadNewBugCustomFields();
            LoadBugs(bugId);
            StatusMessage = "Bug created.";
        }
        catch (Exception ex)
        {
            ShowBugError("Bug Creation Error", ex);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUpdateBugStatus))]
    private void UpdateBugStatus()
    {
        if (SelectedBug is null || SelectedBugStatus is null)
        {
            return;
        }

        try
        {
            bugReportService.UpdateStatus(new UpdateBugStatusRequest(
                SelectedBug.Id,
                SelectedBugStatus.Value,
                userContext.UserName));

            LoadBugs(SelectedBug.Id);
            StatusMessage = "Bug status updated.";
        }
        catch (Exception ex)
        {
            ShowBugError("Bug Update Error", ex);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenSelectedBug))]
    private void OpenBugDetails(BugReportItemViewModel? bug)
    {
        var targetBug = bug ?? SelectedBug;
        if (targetBug is null)
        {
            return;
        }

        SelectedBug = targetBug;
        DetailBugTitle = targetBug.Title;
        DetailBugDescription = targetBug.Description;
        DetailBugSeverity = targetBug.Severity;
        DetailBugPriority = targetBug.Priority;
        DetailBugFoundInVersion = targetBug.FoundInVersion;
        DetailBugBuildNumber = targetBug.BuildNumber;
        DetailSelectedBugStatus = BugStatuses.Single(option => option.Value == targetBug.Status);
        LoadSelectedBugAttachments();
        LoadBugCustomFields(targetBug.Id);
        BugDetailsDrawerVisibility = Visibility.Visible;
    }

    [RelayCommand]
    private void CloseBugDetails()
    {
        BugDetailsDrawerVisibility = Visibility.Collapsed;
    }

    [RelayCommand(CanExecute = nameof(CanSaveBugDetails))]
    private void SaveBugDetails()
    {
        if (SelectedBug is null || DetailSelectedBugStatus is null)
        {
            return;
        }

        try
        {
            bugReportService.UpdateBug(new UpdateBugReportRequest(
                SelectedBug.Id,
                DetailBugTitle,
                DetailBugDescription,
                DetailSelectedBugStatus.Value,
                DetailBugSeverity,
                DetailBugPriority,
                DetailBugFoundInVersion,
                DetailBugBuildNumber,
                userContext.UserName));

            SaveCustomFields(BugCustomFields, SelectedBug.Id);
            LoadBugs(SelectedBug.Id);
            LoadBugCustomFields(SelectedBug.Id);
            StatusMessage = "Bug details saved.";
        }
        catch (Exception ex)
        {
            ShowBugError("Bug Update Error", ex);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenSelectedBug))]
    private void OpenBugDiscussion(BugReportItemViewModel? bug)
    {
        var targetBug = bug ?? SelectedBug;
        if (targetBug is null)
        {
            return;
        }

        SelectedBug = targetBug;
        OpenDiscussion(EntityReferenceType.BugReport, targetBug.Id, $"Bug discussion: {targetBug.Title}");
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

    [RelayCommand(CanExecute = nameof(CanAddBugAttachment))]
    private void AddBugAttachment()
    {
        if (SelectedBug is null)
        {
            return;
        }

        var sourceFilePath = filePickerService.PickAttachmentFile();
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return;
        }

        try
        {
            attachmentService.AddAttachment(new AddAttachmentRequest(
                EntityReferenceType.BugReport,
                SelectedBug.Id,
                sourceFilePath,
                userContext.UserName));

            LoadSelectedBugAttachments();
            StatusMessage = "Bug attachment added.";
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Bug Attachment Error", ex.Message);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void OpenBugAttachment(AttachmentItemViewModel? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        try
        {
            fileLauncherService.OpenFile(attachment.AbsolutePath);
            StatusMessage = "Bug attachment opened.";
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Open Attachment Error", ex.Message);
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void DeleteBugAttachment(AttachmentItemViewModel? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        try
        {
            attachmentService.DeleteAttachment(attachment.Id);
            LoadSelectedBugAttachments();
            StatusMessage = "Bug attachment deleted.";
        }
        catch (Exception ex)
        {
            errorDialogService.ShowError("Delete Attachment Error", ex.Message);
            StatusMessage = ex.Message;
        }
    }

    private bool CanCreateBug()
    {
        return !string.IsNullOrWhiteSpace(NewBugTitle);
    }

    private bool CanUpdateBugStatus()
    {
        return SelectedBug is not null && SelectedBugStatus is not null;
    }

    private bool CanOpenSelectedBug(BugReportItemViewModel? bug)
    {
        return bug is not null || SelectedBug is not null;
    }

    private bool CanSaveBugDetails()
    {
        return SelectedBug is not null
            && DetailSelectedBugStatus is not null
            && !string.IsNullOrWhiteSpace(DetailBugTitle);
    }

    private bool CanSaveDiscussionMessage()
    {
        return discussionEntityId != Guid.Empty && !string.IsNullOrWhiteSpace(DiscussionMessage);
    }

    private bool CanAddBugAttachment()
    {
        return SelectedBug is not null;
    }

    private void LoadBugs(Guid? selectedBugId = null)
    {
        allBugs = bugReportService.GetBugs()
            .Select(MapBug)
            .ToList();

        RefreshFilterOptions();
        ApplyBugFilters(selectedBugId);
    }

    private void ApplyBugFilters(Guid? selectedBugId = null)
    {
        var filteredBugs = allBugs.AsEnumerable();
        var selectedStatus = SelectedBugStatusFilter?.Value;
        if (selectedStatus is not null)
        {
            filteredBugs = filteredBugs.Where(bug => bug.Status == selectedStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(SelectedSeverityFilter)
            && !string.Equals(SelectedSeverityFilter, AllSeveritiesFilter, StringComparison.Ordinal))
        {
            filteredBugs = filteredBugs.Where(bug =>
                string.Equals(bug.Severity, SelectedSeverityFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedPriorityFilter)
            && !string.Equals(SelectedPriorityFilter, AllPrioritiesFilter, StringComparison.Ordinal))
        {
            filteredBugs = filteredBugs.Where(bug =>
                string.Equals(bug.Priority, SelectedPriorityFilter, StringComparison.OrdinalIgnoreCase));
        }

        var preferredBugId = selectedBugId ?? SelectedBug?.Id;
        var bugs = filteredBugs.ToList();

        Bugs.Clear();
        foreach (var bug in bugs)
        {
            Bugs.Add(bug);
        }

        SelectedBug = preferredBugId is null
            ? Bugs.FirstOrDefault()
            : Bugs.FirstOrDefault(bug => bug.Id == preferredBugId) ?? Bugs.FirstOrDefault();
    }

    private void RefreshFilterOptions()
    {
        var selectedSeverity = SelectedSeverityFilter;
        var selectedPriority = SelectedPriorityFilter;

        SeverityFilters.Clear();
        SeverityFilters.Add(AllSeveritiesFilter);
        foreach (var severity in allBugs
                     .Select(bug => bug.Severity)
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value))
        {
            SeverityFilters.Add(severity);
        }

        PriorityFilters.Clear();
        PriorityFilters.Add(AllPrioritiesFilter);
        foreach (var priority in allBugs
                     .Select(bug => bug.Priority)
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value))
        {
            PriorityFilters.Add(priority);
        }

        SelectedSeverityFilter = SeverityFilters.Contains(selectedSeverity, StringComparer.OrdinalIgnoreCase)
            ? selectedSeverity
            : AllSeveritiesFilter;
        SelectedPriorityFilter = PriorityFilters.Contains(selectedPriority, StringComparer.OrdinalIgnoreCase)
            ? selectedPriority
            : AllPrioritiesFilter;
    }

    private void LoadSelectedBugAttachments()
    {
        BugAttachments.Clear();

        if (SelectedBug is null)
        {
            return;
        }

        foreach (var attachment in attachmentService
                     .GetAttachments(EntityReferenceType.BugReport, SelectedBug.Id)
                     .Select(MapAttachment))
        {
            BugAttachments.Add(attachment);
        }
    }

    private void LoadBugCustomFields(Guid bugId)
    {
        BugCustomFields.Clear();

        foreach (var field in customFieldValueService
                     .GetValues(EntityReferenceType.BugReport, bugId, [])
                     .Select(MapCustomField))
        {
            BugCustomFields.Add(field);
        }
    }

    private void LoadNewBugCustomFields()
    {
        NewBugCustomFields.Clear();

        foreach (var field in customFieldValueService
                     .GetValues(EntityReferenceType.BugReport, Guid.Empty, [])
                     .Select(MapCustomField))
        {
            NewBugCustomFields.Add(field);
        }
    }

    private void SaveCustomFields(IEnumerable<CustomFieldValueItemViewModel> fields, Guid bugId)
    {
        foreach (var field in fields)
        {
            customFieldValueService.SaveValue(new SaveCustomFieldValueRequest(
                field.FieldDefinitionId,
                EntityReferenceType.BugReport,
                bugId,
                field.Value,
                userContext.UserName));
        }
    }

    private static void ValidateRequiredCustomFields(IEnumerable<CustomFieldValueItemViewModel> fields)
    {
        var missingField = fields.FirstOrDefault(field =>
            field.IsRequired && string.IsNullOrWhiteSpace(field.Value));

        if (missingField is not null)
        {
            throw new InvalidOperationException($"Field '{missingField.Name}' is required.");
        }
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

    private void ShowBugError(string fallbackTitle, Exception exception)
    {
        var title = exception is DuplicateBugTitleException
            ? "Duplicate Bug"
            : fallbackTitle;

        errorDialogService.ShowError(title, exception.Message);
    }

    private static BugReportItemViewModel MapBug(BugReportItem bug)
    {
        return new BugReportItemViewModel(
            bug.Id,
            bug.Title,
            bug.Description,
            bug.Status,
            bug.Severity,
            bug.Priority,
            bug.FoundInVersion,
            bug.BuildNumber,
            bug.CreatedBy,
            bug.CreatedAt,
            bug.UpdatedBy,
            bug.UpdatedAt,
            bug.LinkedEntityType,
            bug.LinkedEntityId,
            bug.LinkedEntityDisplayName);
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
}
