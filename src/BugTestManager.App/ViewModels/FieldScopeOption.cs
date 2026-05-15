using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed partial class FieldScopeOption : ObservableObject
{
    public FieldScopeOption(
        CustomFieldScopeType scopeType,
        Guid? scopeEntityId,
        string displayName,
        bool isSelected = false)
    {
        ScopeType = scopeType;
        ScopeEntityId = scopeEntityId;
        DisplayName = displayName;
        IsSelected = isSelected;
    }

    public CustomFieldScopeType ScopeType { get; }

    public Guid? ScopeEntityId { get; }

    public string DisplayName { get; }

    [ObservableProperty]
    private bool isSelected;
}
