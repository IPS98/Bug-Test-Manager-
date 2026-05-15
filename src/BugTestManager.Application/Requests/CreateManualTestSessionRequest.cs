namespace BugTestManager.Application.Requests;

public sealed record CreateManualTestSessionRequest(
    string Name,
    string TestedVersion,
    string BuildNumber,
    string Notes,
    string CreatedBy,
    Guid? ProjectId = null);
