using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record BugReportItem(
    Guid Id,
    string Title,
    string Description,
    BugStatus Status,
    string Severity,
    string Priority,
    string FoundInVersion,
    string BuildNumber,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string UpdatedBy,
    DateTimeOffset UpdatedAt,
    EntityReferenceType? LinkedEntityType,
    Guid? LinkedEntityId,
    string LinkedEntityDisplayName);
