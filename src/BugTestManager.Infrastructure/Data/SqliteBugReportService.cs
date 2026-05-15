using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.Exceptions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteBugReportService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : IBugReportService
{
    public IReadOnlyList<BugReportItem> GetBugs(Guid? projectId = null)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var resolvedProjectId = ResolveProjectId(projectId);

        return dbContext.BugReports
            .AsNoTracking()
            .Where(bug => bug.ProjectId == resolvedProjectId)
            .ToList()
            .OrderByDescending(bug => bug.UpdatedAt)
            .Select(MapBug)
            .ToList();
    }

    public Guid CreateBug(CreateBugReportRequest request)
    {
        var title = Require(request.Title, "Title");
        var createdBy = Require(request.CreatedBy, "Created by");
        var projectId = ResolveProjectId(request.ProjectId);
        using var dbContext = dbContextFactory.CreateDbContext();

        EnsureUniqueTitle(dbContext, projectId, title, ignoredBugId: null);

        var now = DateTimeOffset.UtcNow;
        var bugId = Guid.NewGuid();
        var bug = new BugReportRecord
        {
            Id = bugId,
            ProjectId = projectId,
            Title = title,
            Description = request.Description.Trim(),
            Status = BugStatus.Open,
            Severity = NormalizeOptional(request.Severity, "Medium"),
            Priority = NormalizeOptional(request.Priority, "Medium"),
            FoundInVersion = request.FoundInVersion.Trim(),
            BuildNumber = request.BuildNumber.Trim(),
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedBy = createdBy,
            UpdatedAt = now,
            LinkedEntityType = request.LinkedEntityType,
            LinkedEntityId = request.LinkedEntityId == Guid.Empty ? null : request.LinkedEntityId,
            LinkedEntityDisplayName = request.LinkedEntityDisplayName.Trim()
        };

        dbContext.BugReports.Add(bug);
        dbContext.SaveChanges();

        return bugId;
    }

    public void UpdateBug(UpdateBugReportRequest request)
    {
        var title = Require(request.Title, "Title");
        var updatedBy = Require(request.UpdatedBy, "Updated by");

        using var dbContext = dbContextFactory.CreateDbContext();
        var bug = dbContext.BugReports.SingleOrDefault(item => item.Id == request.BugId)
            ?? throw new InvalidOperationException("Selected bug was not found.");

        EnsureUniqueTitle(dbContext, bug.ProjectId, title, bug.Id);

        bug.Title = title;
        bug.Description = request.Description.Trim();
        bug.Status = request.Status;
        bug.Severity = NormalizeOptional(request.Severity, "Medium");
        bug.Priority = NormalizeOptional(request.Priority, "Medium");
        bug.FoundInVersion = request.FoundInVersion.Trim();
        bug.BuildNumber = request.BuildNumber.Trim();
        bug.UpdatedBy = updatedBy;
        bug.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.SaveChanges();
    }

    public void UpdateStatus(UpdateBugStatusRequest request)
    {
        var updatedBy = Require(request.UpdatedBy, "Updated by");

        using var dbContext = dbContextFactory.CreateDbContext();
        var bug = dbContext.BugReports.SingleOrDefault(item => item.Id == request.BugId)
            ?? throw new InvalidOperationException("Selected bug was not found.");

        bug.Status = request.Status;
        bug.UpdatedBy = updatedBy;
        bug.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.SaveChanges();
    }

    private static BugReportItem MapBug(BugReportRecord bug)
    {
        return new BugReportItem(
            bug.Id,
            bug.Title,
            bug.Description,
            bug.Status,
            bug.Severity,
            bug.Priority,
            bug.FoundInVersion,
            bug.BuildNumber,
            bug.CreatedBy,
            bug.CreatedAt,
            bug.UpdatedBy,
            bug.UpdatedAt,
            bug.LinkedEntityType,
            bug.LinkedEntityId,
            bug.LinkedEntityDisplayName);
    }

    private static string Require(string value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", nameof(value));
        }

        return value.Trim();
    }

    private static string NormalizeOptional(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static string NormalizeTitle(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static void EnsureUniqueTitle(
        BugTestManagerDbContext dbContext,
        Guid projectId,
        string title,
        Guid? ignoredBugId)
    {
        var normalizedTitle = NormalizeTitle(title);
        var titleAlreadyExists = dbContext.BugReports
            .AsNoTracking()
            .Where(bug => bug.ProjectId == projectId && (ignoredBugId == null || bug.Id != ignoredBugId.Value))
            .Select(bug => bug.Title)
            .ToList()
            .Any(existingTitle => NormalizeTitle(existingTitle) == normalizedTitle);

        if (titleAlreadyExists)
        {
            throw new DuplicateBugTitleException(title);
        }
    }

    private static Guid ResolveProjectId(Guid? projectId)
    {
        return projectId ?? ProjectDefaults.DefaultProjectId;
    }
}
