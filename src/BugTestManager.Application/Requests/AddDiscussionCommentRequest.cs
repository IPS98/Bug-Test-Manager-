using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Requests;

public sealed record AddDiscussionCommentRequest(
    EntityReferenceType EntityType,
    Guid EntityId,
    string Message,
    string CreatedBy);
