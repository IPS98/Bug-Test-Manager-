namespace BugTestManager.Application.ReadModels;

public sealed record ProjectItem(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt);
