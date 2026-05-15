namespace BugTestManager.Application.Requests;

public sealed record CreateManualTestCaseRequest(
    Guid TestSectionResultId,
    string Title,
    string ExpectedResult);
