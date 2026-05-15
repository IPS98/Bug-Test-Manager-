using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class DiscussionReadStateRecord
{
    public Guid Id { get; set; }

    public EntityReferenceType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTimeOffset LastReadAt { get; set; }
}
