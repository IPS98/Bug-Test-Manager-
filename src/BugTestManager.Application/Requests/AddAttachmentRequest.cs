using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record AddAttachmentRequest(
    EntityReferenceType EntityType,
    Guid EntityId,
    string SourceFilePath,
    string UploadedBy);
