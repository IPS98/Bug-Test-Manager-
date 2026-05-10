namespace BugTestManager.Application.Requests;

public sealed record UpdateTemplateSectionRequest(
    Guid SectionId,
    string Name,
    string Category);
