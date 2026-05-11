using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record UpdateBugReportRequest(
    Guid BugId,
    string Title,
    string Description,
    BugStatus Status,
    string Severity,
    string Priority,
    string FoundInVersion,
    string BuildNumber,
    string UpdatedBy);
