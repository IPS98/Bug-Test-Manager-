using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record AttachmentItem(
    Guid Id,
    EntityReferenceType EntityType,
    Guid EntityId,
    string OriginalFileName,
    string StoredFileName,
    string RelativePath,
    string ContentType,
    long SizeBytes,
    string UploadedBy,
    DateTimeOffset UploadedAt);
