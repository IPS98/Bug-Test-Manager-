using BugTestManager.Domain.Entities;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Tests;

public sealed class AttachmentTests
{
    [Fact]
    public void Constructor_AttachesFileMetadataToEntity()
    {
        var entityId = Guid.NewGuid();

        var attachment = new Attachment(
            EntityReferenceType.TestStepResult,
            entityId,
            "photo.png",
            "stored-photo.png",
            "attachments/2026/05/stored-photo.png",
            "image/png",
            12345,
            "checksum");

        Assert.Equal(EntityReferenceType.TestStepResult, attachment.EntityType);
        Assert.Equal(entityId, attachment.EntityId);
        Assert.Equal("photo.png", attachment.OriginalFileName);
        Assert.Equal(12345, attachment.SizeBytes);
    }

    [Fact]
    public void Constructor_DoesNotAllowNegativeFileSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Attachment(
                EntityReferenceType.BugReport,
                Guid.NewGuid(),
                "bug.png",
                "stored-bug.png",
                "attachments/stored-bug.png",
                "image/png",
                -1));
    }
}
