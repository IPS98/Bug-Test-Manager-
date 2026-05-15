namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class ProjectRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
