using BugTestManager.Domain.Common;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Entities;

public sealed class CustomFieldValue
{
    public CustomFieldValue(Guid fieldDefinitionId, EntityReferenceType entityType, Guid entityId, string valueJson)
    {
        Id = Guid.NewGuid();
        FieldDefinitionId = Guard.Required(fieldDefinitionId, nameof(fieldDefinitionId), "Field definition id");
        EntityType = entityType;
        EntityId = Guard.Required(entityId, nameof(entityId), "Entity id");
        ValueJson = Guard.Required(valueJson, nameof(valueJson), "Field value");
    }

    public Guid Id { get; }

    public Guid FieldDefinitionId { get; }

    public EntityReferenceType EntityType { get; }

    public Guid EntityId { get; }

    public string ValueJson { get; }
}
