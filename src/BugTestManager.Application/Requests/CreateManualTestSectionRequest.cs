namespace BugTestManager.Application.Requests;

public sealed record CreateManualTestSectionRequest(
    Guid TestSessionId,
    string Name,
    string Category);
