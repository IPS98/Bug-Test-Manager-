using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record UpdateBugStatusRequest(
    Guid BugId,
    BugStatus Status,
    string UpdatedBy);
