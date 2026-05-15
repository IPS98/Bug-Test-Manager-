using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record CustomFieldValueScopeItem(
    CustomFieldScopeType ScopeType,
    Guid ScopeEntityId);
