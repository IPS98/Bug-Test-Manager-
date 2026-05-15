using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class CustomFieldValueRecord
{
    public Guid Id { get; set; }

    public Guid FieldDefinitionId { get; set; }

    public EntityReferenceType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string ValueJson { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public CustomFieldDefinitionRecord? FieldDefinition { get; set; }
}
