using BugTestManager.Domain.Enums;

namespace BugTestManager.App.ViewModels;

public sealed class FieldDefinitionItemViewModel
{
    public FieldDefinitionItemViewModel(
        Guid id,
        EntityReferenceType targetEntityType,
        string name,
        FieldType fieldType,
        bool isRequired,
        int sortOrder,
        CustomFieldScopeType scopeType,
        Guid? scopeEntityId,
        string scopeDisplayName,
        bool isActive,
        IReadOnlyList<string> options,
        IReadOnlyList<FieldScopeOption> scopes)
    {
        Id = id;
        TargetEntityType = targetEntityType;
        Name = name;
        FieldType = fieldType;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        ScopeType = scopeType;
        ScopeEntityId = scopeEntityId;
        ScopeDisplayName = scopeDisplayName;
        IsActive = isActive;
        Options = options;
        Scopes = scopes;
    }

    public Guid Id { get; }

    public EntityReferenceType TargetEntityType { get; }

    public string Name { get; }

    public FieldType FieldType { get; }

    public bool IsRequired { get; }

    public int SortOrder { get; }

    public CustomFieldScopeType ScopeType { get; }

    public Guid? ScopeEntityId { get; }

    public string ScopeDisplayName { get; }

    public bool IsActive { get; }

    public IReadOnlyList<string> Options { get; }

    public IReadOnlyList<FieldScopeOption> Scopes { get; }

    public string TargetDisplay => FieldDisplayNames.ForTarget(TargetEntityType);

    public string FieldTypeDisplay => FieldDisplayNames.ForFieldType(FieldType);

    public string RequirementDisplay => IsRequired ? "Required" : "Optional";

    public string StatusDisplay => IsActive ? "Active" : "Archived";

    public string OptionsDisplay => Options.Count == 0 ? "No options" : string.Join(", ", Options);
}
