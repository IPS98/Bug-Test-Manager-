namespace BugTestManager.Application.Requests;

public sealed record CreateTemplateSectionRequest(
    Guid TestSuiteId,
    Guid? TestSuiteRevisionId,
    string Name,
    string Category);
