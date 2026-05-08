using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TestTemplate
{
    private readonly List<TemplateRevision> revisions = [];

    public TestTemplate(Guid testSuiteId, Guid? testSuiteRevisionId, string name, string? description = null)
    {
        Id = Guid.NewGuid();
        TestSuiteId = Guard.Required(testSuiteId, nameof(testSuiteId), "Test suite id");
        TestSuiteRevisionId = testSuiteRevisionId;
        Name = Guard.Required(name, nameof(name), "Template name");
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;
    }

    public Guid Id { get; }

    public Guid TestSuiteId { get; }

    public Guid? TestSuiteRevisionId { get; }

    public string Name { get; }

    public string Description { get; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<TemplateRevision> Revisions => revisions.AsReadOnly();

    public TemplateRevision AddRevision(string revisionName, string? description = null)
    {
        if (revisions.Any(revision => string.Equals(revision.RevisionName, revisionName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Template revision '{revisionName}' already exists for template '{Name}'.");
        }

        var revision = new TemplateRevision(Id, revisionName, description);
        revisions.Add(revision);

        return revision;
    }

    public void Archive()
    {
        IsActive = false;
    }
}
