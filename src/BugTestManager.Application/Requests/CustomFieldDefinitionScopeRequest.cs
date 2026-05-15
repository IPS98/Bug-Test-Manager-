using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record CustomFieldDefinitionScopeRequest(
    CustomFieldScopeType ScopeType,
    Guid? ScopeEntityId,
    string DisplayName);
