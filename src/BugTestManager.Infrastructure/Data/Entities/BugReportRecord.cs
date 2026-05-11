using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class BugReportRecord
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public BugStatus Status { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string FoundInVersion { get; set; } = string.Empty;

    public string BuildNumber { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public EntityReferenceType? LinkedEntityType { get; set; }

    public Guid? LinkedEntityId { get; set; }

    public string LinkedEntityDisplayName { get; set; } = string.Empty;
}
