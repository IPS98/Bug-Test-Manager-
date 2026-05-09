namespace BugTestManager.Application.Requests;

public sealed record CreateTestCaseTemplateRequest(
    Guid TemplateSectionId,
    string Title,
    string ExpectedResult);
