using System.Windows;
using BugTestManager.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed partial class CustomFieldValueItemViewModel : ObservableObject
{
    public CustomFieldValueItemViewModel(
        Guid fieldDefinitionId,
        EntityReferenceType entityType,
        Guid entityId,
        string name,
        FieldType fieldType,
        bool isRequired,
        int sortOrder,
        IReadOnlyList<string> options,
        string value)
    {
        FieldDefinitionId = fieldDefinitionId;
        EntityType = entityType;
        EntityId = entityId;
        Name = name;
        FieldType = fieldType;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        Options = options;
        this.value = value;
    }

    public Guid FieldDefinitionId { get; }

    public EntityReferenceType EntityType { get; }

    public Guid EntityId { get; }

    public string Name { get; }

    public FieldType FieldType { get; }

    public bool IsRequired { get; }

    public int SortOrder { get; }

    public IReadOnlyList<string> Options { get; }

    [ObservableProperty]
    private string value = string.Empty;

    public bool BoolValue
    {
        get => bool.TryParse(Value, out var parsedValue) && parsedValue;
        set => Value = value.ToString().ToLowerInvariant();
    }

    public string HeaderDisplay => IsRequired ? $"{Name} *" : Name;

    public string FieldTypeDisplay => FieldDisplayNames.ForFieldType(FieldType);

    public string OptionsDisplay => Options.Count == 0 ? string.Empty : $"Options: {string.Join(", ", Options)}";

    public Visibility OptionsVisibility => Options.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(BoolValue));
    }
}
