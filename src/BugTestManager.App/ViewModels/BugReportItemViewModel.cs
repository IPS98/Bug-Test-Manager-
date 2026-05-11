using System.Windows.Media;
using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public sealed class BugReportItemViewModel
{
    public BugReportItemViewModel(
        Guid id,
        string title,
        string description,
        BugStatus status,
        string severity,
        string priority,
        string foundInVersion,
        string buildNumber,
        string createdBy,
        DateTimeOffset createdAt,
        string updatedBy,
        DateTimeOffset updatedAt,
        EntityReferenceType? linkedEntityType,
        Guid? linkedEntityId,
        string linkedEntityDisplayName)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = status;
        Severity = severity;
        Priority = priority;
        FoundInVersion = foundInVersion;
        BuildNumber = buildNumber;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        LinkedEntityType = linkedEntityType;
        LinkedEntityId = linkedEntityId;
        LinkedEntityDisplayName = linkedEntityDisplayName;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string Description { get; }

    public BugStatus Status { get; }

    public string Severity { get; }

    public string Priority { get; }

    public string FoundInVersion { get; }

    public string BuildNumber { get; }

    public string CreatedBy { get; }

    public DateTimeOffset CreatedAt { get; }

    public string UpdatedBy { get; }

    public DateTimeOffset UpdatedAt { get; }

    public EntityReferenceType? LinkedEntityType { get; }

    public Guid? LinkedEntityId { get; }

    public string LinkedEntityDisplayName { get; }

    public string StatusDisplay => BugStatusDisplayNames.ForStatus(Status);

    public Brush StatusBackground => BugStatusDisplayNames.BackgroundForStatus(Status);

    public Brush StatusForeground => BugStatusDisplayNames.ForegroundForStatus(Status);

    public string VersionDisplay => string.IsNullOrWhiteSpace(FoundInVersion) ? "Version: -" : $"Version: {FoundInVersion}";

    public string BuildDisplay => string.IsNullOrWhiteSpace(BuildNumber) ? "Build: -" : $"Build: {BuildNumber}";

    public string SeverityDisplay => $"Severity: {Severity}";

    public string PriorityDisplay => $"Priority: {Priority}";

    public string UpdatedAtDisplay => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string UpdatedDisplay => $"Updated: {UpdatedAtDisplay}";

    public string LinkDisplay => string.IsNullOrWhiteSpace(LinkedEntityDisplayName)
        ? "No linked test item"
        : $"Linked to: {LinkedEntityDisplayName}";
}
