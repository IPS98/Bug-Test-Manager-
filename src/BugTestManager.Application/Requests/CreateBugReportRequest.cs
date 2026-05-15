using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record CreateBugReportRequest(
    string Title,
    string Description,
    string Severity,
    string Priority,
    string FoundInVersion,
    string BuildNumber,
    string CreatedBy,
    EntityReferenceType? LinkedEntityType = null,
    Guid? LinkedEntityId = null,
    string LinkedEntityDisplayName = "",
    Guid? ProjectId = null);
