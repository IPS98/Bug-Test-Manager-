using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Abstractions;

public interface IDiscussionService
{
    IReadOnlyList<DiscussionCommentItem> GetComments(EntityReferenceType entityType, Guid entityId);

    Guid AddComment(AddDiscussionCommentRequest request);

    void UpdateComment(UpdateDiscussionCommentRequest request);

    void DeleteComment(Guid commentId);
}
