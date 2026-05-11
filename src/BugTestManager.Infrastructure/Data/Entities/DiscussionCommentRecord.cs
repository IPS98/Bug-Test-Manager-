using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.Data.Entities;

public sealed class DiscussionCommentRecord
{
    public Guid Id { get; set; }

    public EntityReferenceType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }
}
