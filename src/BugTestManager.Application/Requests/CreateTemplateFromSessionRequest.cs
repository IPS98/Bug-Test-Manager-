namespace BugTestManager.Application.Requests;

public sealed record CreateTemplateFromSessionRequest(
    Guid TestSessionId,
    string TemplateName,
    string Description,
    Guid? ProjectId = null);
