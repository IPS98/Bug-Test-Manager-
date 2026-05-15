namespace BugTestManager.Application.Requests;

public sealed record UpdateTestSuiteRequest(
    Guid TestSuiteId,
    string Name,
    string Description,
    bool RevisionIsRequired,
    string? InitialRevisionName = null);
