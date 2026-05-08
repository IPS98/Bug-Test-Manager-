using BugTestManager.Domain.Entities;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Tests;

public sealed class TagTests
{
    [Fact]
    public void Constructor_RequiresName()
    {
        Assert.Throws<ArgumentException>(() => new Tag(" "));
    }

    [Fact]
    public void EntityTag_ConnectsTagToEntity()
    {
        var tag = new Tag("Critical", "#DC2626");
        var entityId = Guid.NewGuid();

        var entityTag = new EntityTag(tag.Id, TaggedEntityType.BugReport, entityId);

        Assert.Equal(tag.Id, entityTag.TagId);
        Assert.Equal(TaggedEntityType.BugReport, entityTag.EntityType);
        Assert.Equal(entityId, entityTag.EntityId);
    }
}
