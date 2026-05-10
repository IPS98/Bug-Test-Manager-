namespace BugTestManager.Application.Requests;

public sealed record CreateTestSessionRequest(
    string Name,
    Guid TestSuiteId,
    Guid? TestSuiteRevisionId,
    string TestedVersion,
    string BuildNumber,
    string Notes,
    string CreatedBy);
