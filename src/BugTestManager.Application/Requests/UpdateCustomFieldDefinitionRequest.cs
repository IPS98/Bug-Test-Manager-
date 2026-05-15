using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record UpdateCustomFieldDefinitionRequest(
    Guid FieldDefinitionId,
    EntityReferenceType TargetEntityType,
    string Name,
    FieldType FieldType,
    bool IsRequired,
    int SortOrder,
    CustomFieldScopeType ScopeType,
    Guid? ScopeEntityId,
    string ScopeDisplayName,
    IReadOnlyCollection<string> Options);
