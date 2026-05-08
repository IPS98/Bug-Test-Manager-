using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class Tag
{
    public Tag(string name, string? color = null)
    {
        Id = Guid.NewGuid();
        Name = Guard.Required(name, nameof(name), "Tag name");
        Color = string.IsNullOrWhiteSpace(color) ? "#64748B" : color.Trim();
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Color { get; }
}
