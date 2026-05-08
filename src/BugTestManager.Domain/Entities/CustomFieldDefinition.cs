using BugTestManager.Domain.Common;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Entities;

public sealed class CustomFieldDefinition
{
    private readonly List<string> options = [];

    public CustomFieldDefinition(
        EntityReferenceType targetEntityType,
        string name,
        FieldType fieldType,
        bool isRequired = false,
        int sortOrder = 0,
        IEnumerable<string>? options = null)
    {
        TargetEntityType = targetEntityType;
        Name = Guard.Required(name, nameof(name), "Field name");
        FieldType = fieldType;
        IsRequired = isRequired;
        SortOrder = Guard.NotNegative(sortOrder, nameof(sortOrder), "Sort order");
        IsActive = true;

        if (options is not null)
        {
            foreach (var option in options)
            {
                AddOption(option);
            }
        }
    }

    public Guid Id { get; } = Guid.NewGuid();

    public EntityReferenceType TargetEntityType { get; }

    public string Name { get; }

    public FieldType FieldType { get; }

    public bool IsRequired { get; }

    public int SortOrder { get; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<string> Options => options.AsReadOnly();

    public void AddOption(string option)
    {
        var normalizedOption = Guard.Required(option, nameof(option), "Field option");

        if (options.Any(existingOption => string.Equals(existingOption, normalizedOption, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Option '{normalizedOption}' already exists for field '{Name}'.");
        }

        options.Add(normalizedOption);
    }

    public void Archive()
    {
        IsActive = false;
    }
}
