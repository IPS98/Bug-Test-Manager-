using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TestSuite
{
    private readonly List<TestSuiteRevision> revisions = [];

    public TestSuite(Guid productId, string name, string? description = null)
    {
        Id = Guid.NewGuid();
        ProductId = Guard.Required(productId, nameof(productId), "Product id");
        Name = Guard.Required(name, nameof(name), "Test suite name");
        Description = description?.Trim() ?? string.Empty;
    }

    public Guid Id { get; }

    public Guid ProductId { get; }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyCollection<TestSuiteRevision> Revisions => revisions.AsReadOnly();

    public TestSuiteRevision AddRevision(string revisionName, DateOnly? effectiveDate = null)
    {
        if (revisions.Any(revision => string.Equals(revision.RevisionName, revisionName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Revision '{revisionName}' already exists for test suite '{Name}'.");
        }

        var revision = new TestSuiteRevision(Id, revisionName, effectiveDate);
        revisions.Add(revision);

        return revision;
    }
}
