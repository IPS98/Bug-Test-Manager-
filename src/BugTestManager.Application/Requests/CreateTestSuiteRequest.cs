namespace BugTestManager.Application.Requests;

public sealed record CreateTestSuiteRequest(
    string Name,
    string Description,
    bool RevisionIsRequired,
    string? InitialRevisionName,
    Guid? ProjectId = null);
