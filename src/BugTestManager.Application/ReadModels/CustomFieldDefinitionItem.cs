using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record CustomFieldDefinitionItem(
    Guid Id,
    Guid ProjectId,
    EntityReferenceType TargetEntityType,
    string Name,
    FieldType FieldType,
    bool IsRequired,
    int SortOrder,
    CustomFieldScopeType ScopeType,
    Guid? ScopeEntityId,
    string ScopeDisplayName,
    bool IsActive,
    IReadOnlyList<string> Options,
    IReadOnlyList<CustomFieldDefinitionScopeItem> Scopes);
