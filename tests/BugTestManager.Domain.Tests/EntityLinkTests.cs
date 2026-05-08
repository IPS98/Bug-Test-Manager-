using BugTestManager.Domain.Entities;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Tests;

public sealed class EntityLinkTests
{
    [Fact]
    public void Constructor_CanLinkBugToTestCase()
    {
        var bugId = Guid.NewGuid();
        var testCaseId = Guid.NewGuid();

        var link = new EntityLink(
            EntityReferenceType.BugReport,
            bugId,
            EntityReferenceType.TestCaseResult,
            testCaseId,
            EntityLinkType.Related);

        Assert.Equal(EntityReferenceType.BugReport, link.SourceEntityType);
        Assert.Equal(bugId, link.SourceEntityId);
        Assert.Equal(EntityReferenceType.TestCaseResult, link.TargetEntityType);
        Assert.Equal(testCaseId, link.TargetEntityId);
        Assert.Equal(EntityLinkType.Related, link.LinkType);
    }

    [Fact]
    public void Constructor_CanTrackCopiedSession()
    {
        var newSessionId = Guid.NewGuid();
        var previousSessionId = Guid.NewGuid();

        var link = new EntityLink(
            EntityReferenceType.TestSession,
            newSessionId,
            EntityReferenceType.TestSession,
            previousSessionId,
            EntityLinkType.CopiedFrom);

        Assert.Equal(EntityLinkType.CopiedFrom, link.LinkType);
    }
}
