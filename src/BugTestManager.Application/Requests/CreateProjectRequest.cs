namespace BugTestManager.Application.Requests;

public sealed record CreateProjectRequest(
    string Name,
    string Description = "");
