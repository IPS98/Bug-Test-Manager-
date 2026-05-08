using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TemplateRevision
{
    private readonly List<TemplateSection> sections = [];

    public TemplateRevision(Guid testTemplateId, string revisionName, string? description = null)
    {
        Id = Guid.NewGuid();
        TestTemplateId = Guard.Required(testTemplateId, nameof(testTemplateId), "Test template id");
        RevisionName = Guard.Required(revisionName, nameof(revisionName), "Template revision name");
        Description = description?.Trim() ?? string.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public Guid TestTemplateId { get; }

    public string RevisionName { get; }

    public string Description { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<TemplateSection> Sections => sections.AsReadOnly();

    public TemplateSection AddSection(string name, string? category = null, Guid? parentSectionId = null, int sortOrder = 0)
    {
        var section = new TemplateSection(Id, name, category, parentSectionId, sortOrder);
        sections.Add(section);

        return section;
    }
}
