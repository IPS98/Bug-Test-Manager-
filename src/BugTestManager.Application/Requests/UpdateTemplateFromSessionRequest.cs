namespace BugTestManager.Application.Requests;

public sealed record UpdateTemplateFromSessionRequest(
    Guid TestSessionId,
    Guid? ProjectId = null);
