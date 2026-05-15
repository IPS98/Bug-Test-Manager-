using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Abstractions;

public interface IDiscussionService
{
    IReadOnlyList<DiscussionCommentItem> GetComments(EntityReferenceType entityType, Guid entityId);

    int GetUnreadCount(EntityReferenceType entityType, Guid entityId, string userName);

    Guid AddComment(AddDiscussionCommentRequest request);

    void MarkRead(EntityReferenceType entityType, Guid entityId, string userName);

    void UpdateComment(UpdateDiscussionCommentRequest request);

    void DeleteComment(Guid commentId);
}
