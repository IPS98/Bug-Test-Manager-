using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record CustomFieldValueItem(
    Guid FieldDefinitionId,
    EntityReferenceType EntityType,
    Guid EntityId,
    string Name,
    FieldType FieldType,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<string> Options,
    string Value);
