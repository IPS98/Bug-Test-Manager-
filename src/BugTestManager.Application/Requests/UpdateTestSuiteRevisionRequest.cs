namespace BugTestManager.Application.Requests;

public sealed record UpdateTestSuiteRevisionRequest(
    Guid TestSuiteRevisionId,
    string Name);
