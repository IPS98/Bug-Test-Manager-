using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TestSuiteRevision
{
    public TestSuiteRevision(Guid testSuiteId, string revisionName, DateOnly? effectiveDate = null)
    {
        Id = Guid.NewGuid();
        TestSuiteId = Guard.Required(testSuiteId, nameof(testSuiteId), "Test suite id");
        RevisionName = Guard.Required(revisionName, nameof(revisionName), "Revision name");
        EffectiveDate = effectiveDate;
    }

    public Guid Id { get; }

    public Guid TestSuiteId { get; }

    public string RevisionName { get; }

    public DateOnly? EffectiveDate { get; }
}
