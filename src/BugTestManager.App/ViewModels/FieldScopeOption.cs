using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public sealed record FieldScopeOption(
    CustomFieldScopeType ScopeType,
    Guid? ScopeEntityId,
    string DisplayName);
