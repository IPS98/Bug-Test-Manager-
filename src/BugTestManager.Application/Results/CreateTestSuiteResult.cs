namespace BugTestManager.Application.Results;

public sealed record CreateTestSuiteResult(Guid TestSuiteId, Guid? InitialRevisionId);
