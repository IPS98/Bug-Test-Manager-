using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public sealed record DiscussionCommentItem(
    Guid Id,
    EntityReferenceType EntityType,
    Guid EntityId,
    string Message,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    string UpdatedBy,
    DateTimeOffset? UpdatedAt);
