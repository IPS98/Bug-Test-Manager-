using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class CustomFieldDefinitionScopeRecord
{
    public Guid Id { get; set; }

    public Guid FieldDefinitionId { get; set; }

    public CustomFieldScopeType ScopeType { get; set; }

    public Guid? ScopeEntityId { get; set; }

    public string ScopeDisplayName { get; set; } = string.Empty;

    public CustomFieldDefinitionRecord? FieldDefinition { get; set; }
}
