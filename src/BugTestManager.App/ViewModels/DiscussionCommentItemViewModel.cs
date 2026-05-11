namespace BugTestManager.App.ViewModels;

public sealed class DiscussionCommentItemViewModel
{
    public DiscussionCommentItemViewModel(
        Guid id,
        string message,
        string createdBy,
        DateTimeOffset createdAt,
        string updatedBy,
        DateTimeOffset? updatedAt)
    {
        Id = id;
        Message = message;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Message { get; }

    public string CreatedBy { get; }

    public DateTimeOffset CreatedAt { get; }

    public string UpdatedBy { get; }

    public DateTimeOffset? UpdatedAt { get; }

    public string HeaderDisplay => $"{CreatedBy} - {CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";

    public string EditedDisplay => UpdatedAt is null
        ? string.Empty
        : $"Edited by {UpdatedBy} - {UpdatedAt.Value.ToLocalTime():yyyy-MM-dd HH:mm}";
}
