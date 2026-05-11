namespace BugTestManager.Application.Requests;

public sealed record UpdateDiscussionCommentRequest(
    Guid CommentId,
    string Message,
    string UpdatedBy);
