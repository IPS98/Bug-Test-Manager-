using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteAttachmentService(
    IDbContextFactory<BugTestManagerDbContext> dbContextFactory,
    string? attachmentRootPath = null)
    : IAttachmentService
{
    private readonly string attachmentRootPath = attachmentRootPath ?? DatabasePaths.GetDefaultAttachmentRootPath();

    public IReadOnlyList<AttachmentItem> GetAttachments(EntityReferenceType entityType, Guid entityId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        return dbContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.EntityType == entityType && attachment.EntityId == entityId)
            .ToList()
            .OrderByDescending(attachment => attachment.UploadedAt)
            .Select(MapAttachment)
            .ToList();
    }

    public Guid AddAttachment(AddAttachmentRequest request)
    {
        if (request.EntityId == Guid.Empty)
        {
            throw new ArgumentException("Entity id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UploadedBy))
        {
            throw new ArgumentException("Uploaded by is required.", nameof(request));
        }

        var sourceFile = new FileInfo(request.SourceFilePath);
        if (!sourceFile.Exists)
        {
            throw new FileNotFoundException("Selected attachment file was not found.", request.SourceFilePath);
        }

        Directory.CreateDirectory(attachmentRootPath);

        var attachmentId = Guid.NewGuid();
        var uploadedAt = DateTimeOffset.UtcNow;
        var relativeDirectory = Path.Combine(uploadedAt.ToString("yyyy"), uploadedAt.ToString("MM"));
        var absoluteDirectory = Path.Combine(attachmentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var storedFileName = $"{attachmentId:N}{sourceFile.Extension}";
        var relativePath = Path.Combine(relativeDirectory, storedFileName);
        var destinationPath = Path.Combine(attachmentRootPath, relativePath);

        sourceFile.CopyTo(destinationPath, overwrite: false);

        var record = new AttachmentRecord
        {
            Id = attachmentId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            OriginalFileName = sourceFile.Name,
            StoredFileName = storedFileName,
            RelativePath = relativePath,
            ContentType = GetContentType(sourceFile.Extension),
            SizeBytes = sourceFile.Length,
            UploadedBy = request.UploadedBy.Trim(),
            UploadedAt = uploadedAt
        };

        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Attachments.Add(record);
        dbContext.SaveChanges();

        return attachmentId;
    }

    private static AttachmentItem MapAttachment(AttachmentRecord attachment)
    {
        return new AttachmentItem(
            attachment.Id,
            attachment.EntityType,
            attachment.EntityId,
            attachment.OriginalFileName,
            attachment.StoredFileName,
            attachment.RelativePath,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedBy,
            attachment.UploadedAt);
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" or ".log" => "text/plain",
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
