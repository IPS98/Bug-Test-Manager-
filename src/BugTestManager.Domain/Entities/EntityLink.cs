using BugTestManager.Domain.Common;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Entities;

public sealed class EntityLink
{
    public EntityLink(
        EntityReferenceType sourceEntityType,
        Guid sourceEntityId,
        EntityReferenceType targetEntityType,
        Guid targetEntityId,
        EntityLinkType linkType)
    {
        Id = Guid.NewGuid();
        SourceEntityType = sourceEntityType;
        SourceEntityId = Guard.Required(sourceEntityId, nameof(sourceEntityId), "Source entity id");
        TargetEntityType = targetEntityType;
        TargetEntityId = Guard.Required(targetEntityId, nameof(targetEntityId), "Target entity id");
        LinkType = linkType;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public EntityReferenceType SourceEntityType { get; }

    public Guid SourceEntityId { get; }

    public EntityReferenceType TargetEntityType { get; }

    public Guid TargetEntityId { get; }

    public EntityLinkType LinkType { get; }

    public DateTimeOffset CreatedAt { get; }
}
