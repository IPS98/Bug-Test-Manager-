using BugTestManager.Domain.Common;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Domain.Entities;

public sealed class Attachment
{
    public Attachment(
        EntityReferenceType entityType,
        Guid entityId,
        string originalFileName,
        string storedFileName,
        string relativePath,
        string contentType,
        long sizeBytes,
        string? checksum = null)
    {
        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), sizeBytes, "Attachment size must not be negative.");
        }

        Id = Guid.NewGuid();
        EntityType = entityType;
        EntityId = Guard.Required(entityId, nameof(entityId), "Entity id");
        OriginalFileName = Guard.Required(originalFileName, nameof(originalFileName), "Original file name");
        StoredFileName = Guard.Required(storedFileName, nameof(storedFileName), "Stored file name");
        RelativePath = Guard.Required(relativePath, nameof(relativePath), "Relative path");
        ContentType = Guard.Required(contentType, nameof(contentType), "Content type");
        SizeBytes = sizeBytes;
        Checksum = checksum?.Trim() ?? string.Empty;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public EntityReferenceType EntityType { get; }

    public Guid EntityId { get; }

    public string OriginalFileName { get; }

    public string StoredFileName { get; }

    public string RelativePath { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public string Checksum { get; }

    public DateTimeOffset UploadedAt { get; }
}
