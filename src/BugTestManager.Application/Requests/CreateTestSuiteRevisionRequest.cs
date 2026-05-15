namespace BugTestManager.Application.Requests;

public sealed record CreateTestSuiteRevisionRequest(
    Guid TestSuiteId,
    string Name,
    Guid? SourceRevisionId);
