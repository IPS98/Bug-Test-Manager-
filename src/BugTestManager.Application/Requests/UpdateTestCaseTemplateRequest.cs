namespace BugTestManager.Application.Requests;

public sealed record UpdateTestCaseTemplateRequest(
    Guid TestCaseId,
    string Title,
    string ExpectedResult);
