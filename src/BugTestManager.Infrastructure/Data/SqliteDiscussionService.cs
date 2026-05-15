using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteDiscussionService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : IDiscussionService
{
    public IReadOnlyList<DiscussionCommentItem> GetComments(EntityReferenceType entityType, Guid entityId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        return dbContext.DiscussionComments
            .AsNoTracking()
            .Where(comment => comment.EntityType == entityType && comment.EntityId == entityId)
            .ToList()
            .OrderBy(comment => comment.CreatedAt)
            .Select(MapComment)
            .ToList();
    }

    public int GetUnreadCount(EntityReferenceType entityType, Guid entityId, string userName)
    {
        if (entityId == Guid.Empty || string.IsNullOrWhiteSpace(userName))
        {
            return 0;
        }

        var normalizedUserName = userName.Trim();
        using var dbContext = dbContextFactory.CreateDbContext();
        var lastReadAt = dbContext.DiscussionReadStates
            .AsNoTracking()
            .Where(readState =>
                readState.EntityType == entityType
                && readState.EntityId == entityId
                && readState.UserName.ToUpper() == normalizedUserName.ToUpper())
            .Select(readState => (DateTimeOffset?)readState.LastReadAt)
            .SingleOrDefault();

        return dbContext.DiscussionComments
            .AsNoTracking()
            .Where(comment => comment.EntityType == entityType && comment.EntityId == entityId)
            .ToList()
            .Count(comment =>
                !string.Equals(comment.CreatedBy, normalizedUserName, StringComparison.OrdinalIgnoreCase)
                && (lastReadAt == null || comment.CreatedAt > lastReadAt.Value));
    }

    public Guid AddComment(AddDiscussionCommentRequest request)
    {
        if (request.EntityId == Guid.Empty)
        {
            throw new ArgumentException("Entity id is required.", nameof(request));
        }

        var message = Require(request.Message, "Comment");
        var createdBy = Require(request.CreatedBy, "Created by");
        var now = DateTimeOffset.UtcNow;
        var commentId = Guid.NewGuid();

        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.DiscussionComments.Add(new DiscussionCommentRecord
        {
            Id = commentId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Message = message,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedBy = string.Empty,
            UpdatedAt = null
        });

        dbContext.SaveChanges();

        return commentId;
    }

    public void MarkRead(EntityReferenceType entityType, Guid entityId, string userName)
    {
        if (entityId == Guid.Empty)
        {
            return;
        }

        var normalizedUserName = Require(userName, "User name");
        using var dbContext = dbContextFactory.CreateDbContext();
        var readState = dbContext.DiscussionReadStates.SingleOrDefault(item =>
            item.EntityType == entityType
            && item.EntityId == entityId
            && item.UserName.ToUpper() == normalizedUserName.ToUpper());

        if (readState is null)
        {
            dbContext.DiscussionReadStates.Add(new DiscussionReadStateRecord
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                UserName = normalizedUserName,
                LastReadAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            readState.LastReadAt = DateTimeOffset.UtcNow;
        }

        dbContext.SaveChanges();
    }

    public void UpdateComment(UpdateDiscussionCommentRequest request)
    {
        var message = Require(request.Message, "Comment");
        var updatedBy = Require(request.UpdatedBy, "Updated by");

        using var dbContext = dbContextFactory.CreateDbContext();
        var comment = dbContext.DiscussionComments.SingleOrDefault(item => item.Id == request.CommentId)
            ?? throw new InvalidOperationException("Selected comment was not found.");

        comment.Message = message;
        comment.UpdatedBy = updatedBy;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.SaveChanges();
    }

    public void DeleteComment(Guid commentId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var comment = dbContext.DiscussionComments.SingleOrDefault(item => item.Id == commentId)
            ?? throw new InvalidOperationException("Selected comment was not found.");

        dbContext.DiscussionComments.Remove(comment);
        dbContext.SaveChanges();
    }

    private static DiscussionCommentItem MapComment(DiscussionCommentRecord comment)
    {
        return new DiscussionCommentItem(
            comment.Id,
            comment.EntityType,
            comment.EntityId,
            comment.Message,
            comment.CreatedBy,
            comment.CreatedAt,
            comment.UpdatedBy,
            comment.UpdatedAt);
    }

    private static string Require(string value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", nameof(value));
        }

        return value.Trim();
    }
}
