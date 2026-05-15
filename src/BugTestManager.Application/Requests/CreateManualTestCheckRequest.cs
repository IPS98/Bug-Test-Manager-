namespace BugTestManager.Application.Requests;

public sealed record CreateManualTestCheckRequest(
    Guid TestCaseResultId,
    string CheckText,
    string ExpectedResult);
