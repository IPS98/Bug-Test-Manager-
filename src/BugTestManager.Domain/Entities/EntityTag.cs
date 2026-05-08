using BugTestManager.Domain.Common;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Entities;

public sealed class EntityTag
{
    public EntityTag(Guid tagId, TaggedEntityType entityType, Guid entityId)
    {
        Id = Guid.NewGuid();
        TagId = Guard.Required(tagId, nameof(tagId), "Tag id");
        EntityType = entityType;
        EntityId = Guard.Required(entityId, nameof(entityId), "Entity id");
    }

    public Guid Id { get; }

    public Guid TagId { get; }

    public TaggedEntityType EntityType { get; }

    public Guid EntityId { get; }
}
