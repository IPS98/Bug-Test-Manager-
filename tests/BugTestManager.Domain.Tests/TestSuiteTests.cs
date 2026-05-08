using BugTestManager.Domain.Entities;

namespace BugTestManager.Domain.Tests;

public sealed class TestSuiteTests
{
    [Fact]
    public void Constructor_RequiresName()
    {
        Assert.Throws<ArgumentException>(() => new TestSuite(Guid.NewGuid(), " "));
    }

    [Fact]
    public void AddRevision_StoresRevisionWithParentTestSuite()
    {
        var testSuite = new TestSuite(Guid.NewGuid(), "Electrical checks");

        var revision = testSuite.AddRevision("Revision A", new DateOnly(2026, 5, 8));

        Assert.Equal(testSuite.Id, revision.TestSuiteId);
        Assert.Equal("Revision A", revision.RevisionName);
        Assert.Single(testSuite.Revisions);
    }

    [Fact]
    public void AddRevision_DoesNotAllowDuplicateRevisionNames()
    {
        var testSuite = new TestSuite(Guid.NewGuid(), "Electrical checks");
        testSuite.AddRevision("Revision A");

        Assert.Throws<InvalidOperationException>(() => testSuite.AddRevision("revision a"));
    }
}
