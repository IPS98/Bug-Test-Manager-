using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record CustomFieldDefinitionScopeItem(
    CustomFieldScopeType ScopeType,
    Guid? ScopeEntityId,
    string DisplayName);
