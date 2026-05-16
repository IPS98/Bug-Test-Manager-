using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.ReadModels;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteReportDataService(
    IDbContextFactory<BugTestManagerDbContext> dbContextFactory,
    string? attachmentRootPath = null)
    : IReportDataService
{
    private readonly string attachmentRootPath = attachmentRootPath ?? DatabasePaths.GetDefaultAttachmentRootPath();

    public TestSessionReportItem GetTestSessionReport(Guid testSessionId, Guid? projectId = null)
    {
        var resolvedProjectId = ResolveProjectId(projectId);
        using var dbContext = dbContextFactory.CreateDbContext();

        var session = dbContext.TestSessions
            .AsNoTracking()
            .Include(item => item.Sections)
            .ThenInclude(section => section.TestCases)
            .ThenInclude(testCase => testCase.Steps)
            .SingleOrDefault(item => item.Id == testSessionId && item.ProjectId == resolvedProjectId)
            ?? throw new InvalidOperationException("Selected test session was not found.");

        var projectName = dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == resolvedProjectId)
            .Select(project => project.Name)
            .SingleOrDefault() ?? "Unknown project";

        var testCaseIds = session.Sections
            .SelectMany(section => section.TestCases)
            .Select(testCase => testCase.Id)
            .ToList();
        var checkIds = session.Sections
            .SelectMany(section => section.TestCases)
            .SelectMany(testCase => testCase.Steps)
            .Select(check => check.Id)
            .ToList();

        var customFieldsByTestCase = LoadCustomFields(dbContext, EntityReferenceType.TestCaseResult, testCaseIds);
        var customFieldsByCheck = LoadCustomFields(dbContext, EntityReferenceType.TestStepResult, checkIds);
        var attachmentsByTestCase = LoadAttachments(dbContext, EntityReferenceType.TestCaseResult, testCaseIds);
        var attachmentsByCheck = LoadAttachments(dbContext, EntityReferenceType.TestStepResult, checkIds);
        var linkedBugs = LoadLinkedBugs(dbContext, resolvedProjectId, testCaseIds, checkIds);

        var reportSections = session.Sections
            .OrderBy(section => section.SortOrder)
            .Select(section => new ReportSectionItem(
                section.Id,
                section.Name,
                section.Category,
                section.SortOrder,
                section.TestCases
                    .OrderBy(testCase => testCase.SortOrder)
                    .Select(testCase => new ReportTestCaseItem(
                        testCase.Id,
                        testCase.Title,
                        testCase.ExpectedResult,
                        testCase.SortOrder,
                        testCase.Status,
                        testCase.LastStatusChangedAt,
                        testCase.Comment,
                        GetDictionaryItems(customFieldsByTestCase, testCase.Id),
                        GetDictionaryItems(attachmentsByTestCase, testCase.Id),
                        testCase.Steps
                            .OrderBy(check => check.SortOrder)
                            .Select(check => new ReportCheckItem(
                                check.Id,
                                check.StepText,
                                check.ExpectedResult,
                                check.SortOrder,
                                check.Status,
                                check.LastStatusChangedAt,
                                check.Comment,
                                GetDictionaryItems(customFieldsByCheck, check.Id),
                                GetDictionaryItems(attachmentsByCheck, check.Id)))
                            .ToList()))
                    .ToList()))
            .ToList();

        return new TestSessionReportItem(
            session.Id,
            session.Name,
            projectName,
            session.TestSuiteName,
            session.TestSuiteRevisionName,
            session.TestedVersion,
            session.BuildNumber,
            session.Notes,
            session.CreatedBy,
            session.CreatedAt,
            BuildSummary(reportSections),
            reportSections,
            linkedBugs);
    }

    private Dictionary<Guid, IReadOnlyList<ReportCustomFieldItem>> LoadCustomFields(
        BugTestManagerDbContext dbContext,
        EntityReferenceType entityType,
        IReadOnlyCollection<Guid> entityIds)
    {
        if (entityIds.Count == 0)
        {
            return [];
        }

        return dbContext.CustomFieldValues
            .AsNoTracking()
            .Include(value => value.FieldDefinition)
            .Where(value => value.EntityType == entityType && entityIds.Contains(value.EntityId))
            .ToList()
            .Where(value => value.FieldDefinition is not null)
            .GroupBy(value => value.EntityId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ReportCustomFieldItem>)group
                    .OrderBy(value => value.FieldDefinition!.SortOrder)
                    .ThenBy(value => value.FieldDefinition!.Name)
                    .Select(value => new ReportCustomFieldItem(
                        value.FieldDefinition!.Name,
                        value.FieldDefinition.FieldType,
                        value.ValueJson,
                        value.UpdatedBy,
                        value.UpdatedAt))
                    .ToList());
    }

    private Dictionary<Guid, IReadOnlyList<ReportAttachmentItem>> LoadAttachments(
        BugTestManagerDbContext dbContext,
        EntityReferenceType entityType,
        IReadOnlyCollection<Guid> entityIds)
    {
        if (entityIds.Count == 0)
        {
            return [];
        }

        return dbContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.EntityType == entityType && entityIds.Contains(attachment.EntityId))
            .ToList()
            .GroupBy(attachment => attachment.EntityId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ReportAttachmentItem>)group
                    .OrderBy(attachment => attachment.UploadedAt)
                    .Select(attachment => new ReportAttachmentItem(
                        attachment.Id,
                        attachment.OriginalFileName,
                        Path.Combine(attachmentRootPath, attachment.RelativePath),
                        attachment.ContentType,
                        attachment.SizeBytes,
                        attachment.UploadedBy,
                        attachment.UploadedAt))
                    .ToList());
    }

    private static IReadOnlyList<ReportBugItem> LoadLinkedBugs(
        BugTestManagerDbContext dbContext,
        Guid projectId,
        IReadOnlyCollection<Guid> testCaseIds,
        IReadOnlyCollection<Guid> checkIds)
    {
        return dbContext.BugReports
            .AsNoTracking()
            .Where(bug =>
                bug.ProjectId == projectId
                && bug.LinkedEntityType != null
                && bug.LinkedEntityId != null
                && ((bug.LinkedEntityType == EntityReferenceType.TestCaseResult && testCaseIds.Contains(bug.LinkedEntityId.Value))
                    || (bug.LinkedEntityType == EntityReferenceType.TestStepResult && checkIds.Contains(bug.LinkedEntityId.Value))))
            .ToList()
            .OrderByDescending(bug => bug.UpdatedAt)
            .Select(bug => new ReportBugItem(
                bug.Id,
                bug.Title,
                bug.Status.ToString(),
                bug.Severity,
                bug.Priority,
                bug.LinkedEntityDisplayName,
                bug.FoundInVersion,
                bug.BuildNumber,
                bug.CreatedBy,
                bug.CreatedAt,
                bug.UpdatedBy,
                bug.UpdatedAt))
            .ToList();
    }

    private static ReportStatusSummaryItem BuildSummary(IReadOnlyList<ReportSectionItem> sections)
    {
        var testCases = sections.SelectMany(section => section.TestCases).ToList();

        return new ReportStatusSummaryItem(
            testCases.Count,
            testCases.Count(testCase => testCase.Status == TestResultStatus.Pass),
            testCases.Count(testCase => testCase.Status == TestResultStatus.Fail),
            testCases.Count(testCase => testCase.Status == TestResultStatus.Blocked),
            testCases.Count(testCase => testCase.Status == TestResultStatus.NotTested),
            testCases.Count(testCase => testCase.Status == TestResultStatus.NotApplicable));
    }

    private static IReadOnlyList<TItem> GetDictionaryItems<TItem>(
        IReadOnlyDictionary<Guid, IReadOnlyList<TItem>> itemsByEntityId,
        Guid entityId)
    {
        return itemsByEntityId.TryGetValue(entityId, out var items)
            ? items
            : [];
    }

    private static Guid ResolveProjectId(Guid? projectId)
    {
        return projectId ?? ProjectDefaults.DefaultProjectId;
    }
}
