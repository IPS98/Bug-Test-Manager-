using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class CustomFieldDefinitionRecord
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public EntityReferenceType TargetEntityType { get; set; }

    public string Name { get; set; } = string.Empty;

    public FieldType FieldType { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }

    public CustomFieldScopeType ScopeType { get; set; }

    public Guid? ScopeEntityId { get; set; }

    public string ScopeDisplayName { get; set; } = "All matching items";

    public bool IsActive { get; set; } = true;

    public string OptionsJson { get; set; } = "[]";

    public List<CustomFieldDefinitionScopeRecord> Scopes { get; set; } = [];
}
