namespace BugTestManager.Application.Requests;

public sealed record CopyTestSessionRequest(
    string Name,
    Guid SourceTestSessionId,
    string TestedVersion,
    string BuildNumber,
    string Notes,
    string CreatedBy,
    Guid? ProjectId = null);
