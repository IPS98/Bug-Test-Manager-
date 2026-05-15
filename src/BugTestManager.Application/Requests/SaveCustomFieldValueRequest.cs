using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record SaveCustomFieldValueRequest(
    Guid FieldDefinitionId,
    EntityReferenceType EntityType,
    Guid EntityId,
    string Value,
    string UpdatedBy);
