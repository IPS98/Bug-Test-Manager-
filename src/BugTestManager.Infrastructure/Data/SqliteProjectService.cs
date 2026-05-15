using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteProjectService(
    IDbContextFactory<BugTestManagerDbContext> dbContextFactory,
    string? attachmentRootPath = null)
    : IProjectService
{
    private readonly string attachmentRootPath = attachmentRootPath ?? DatabasePaths.GetDefaultAttachmentRootPath();

    public IReadOnlyList<ProjectItem> GetProjects()
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        return dbContext.Projects
            .AsNoTracking()
            .OrderBy(project => project.Name)
            .Select(project => new ProjectItem(
                project.Id,
                project.Name,
                project.Description,
                project.CreatedAt))
            .ToList();
    }

    public Guid CreateProject(CreateProjectRequest request)
    {
        var name = Require(request.Name, "Project name");
        using var dbContext = dbContextFactory.CreateDbContext();

        var duplicateExists = dbContext.Projects.Any(project => project.Name.ToUpper() == name.ToUpper());
        if (duplicateExists)
        {
            throw new InvalidOperationException($"Project '{name}' already exists.");
        }

        var projectId = Guid.NewGuid();
        dbContext.Projects.Add(new ProjectRecord
        {
            Id = projectId,
            Name = name,
            Description = request.Description.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        });

        dbContext.SaveChanges();
        return projectId;
    }

    public void DeleteProject(Guid projectId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var project = dbContext.Projects.SingleOrDefault(item => item.Id == projectId)
            ?? throw new InvalidOperationException("Selected project was not found.");

        if (dbContext.Projects.Count() <= 1)
        {
            throw new InvalidOperationException("At least one project must remain.");
        }

        var testSuiteIds = dbContext.TestSuites
            .Where(suite => suite.ProjectId == projectId)
            .Select(suite => suite.Id)
            .ToList();
        var revisionIds = dbContext.TestSuiteRevisions
            .Where(revision => testSuiteIds.Contains(revision.TestSuiteId))
            .Select(revision => revision.Id)
            .ToList();
        var templateSectionIds = dbContext.TemplateSections
            .Where(section => testSuiteIds.Contains(section.TestSuiteId))
            .Select(section => section.Id)
            .ToList();
        var testCaseTemplateIds = dbContext.TestCaseTemplates
            .Where(testCase => templateSectionIds.Contains(testCase.TemplateSectionId))
            .Select(testCase => testCase.Id)
            .ToList();
        var testStepTemplateIds = dbContext.TestStepTemplates
            .Where(step => testCaseTemplateIds.Contains(step.TestCaseTemplateId))
            .Select(step => step.Id)
            .ToList();

        var testSessionIds = dbContext.TestSessions
            .Where(session => session.ProjectId == projectId)
            .Select(session => session.Id)
            .ToList();
        var testSectionResultIds = dbContext.TestSectionResults
            .Where(section => testSessionIds.Contains(section.TestSessionId))
            .Select(section => section.Id)
            .ToList();
        var testCaseResultIds = dbContext.TestCaseResults
            .Where(testCase => testSectionResultIds.Contains(testCase.TestSectionResultId))
            .Select(testCase => testCase.Id)
            .ToList();
        var testStepResultIds = dbContext.TestStepResults
            .Where(step => testCaseResultIds.Contains(step.TestCaseResultId))
            .Select(step => step.Id)
            .ToList();

        var bugReportIds = dbContext.BugReports
            .Where(bug => bug.ProjectId == projectId)
            .Select(bug => bug.Id)
            .ToList();
        var fieldDefinitionIds = dbContext.CustomFieldDefinitions
            .Where(field => field.ProjectId == projectId)
            .Select(field => field.Id)
            .ToList();

        DeleteEntitySideData(dbContext, EntityReferenceType.TestSuite, testSuiteIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestSuiteRevision, revisionIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TemplateSection, templateSectionIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestCaseTemplate, testCaseTemplateIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestStepTemplate, testStepTemplateIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestSession, testSessionIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestSectionResult, testSectionResultIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestCaseResult, testCaseResultIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.TestStepResult, testStepResultIds);
        DeleteEntitySideData(dbContext, EntityReferenceType.BugReport, bugReportIds);

        dbContext.CustomFieldValues
            .Where(value => fieldDefinitionIds.Contains(value.FieldDefinitionId))
            .ExecuteDelete();
        dbContext.CustomFieldDefinitionScopes
            .Where(scope => fieldDefinitionIds.Contains(scope.FieldDefinitionId))
            .ExecuteDelete();
        dbContext.CustomFieldDefinitions
            .Where(field => field.ProjectId == projectId)
            .ExecuteDelete();
        dbContext.BugReports
            .Where(bug => bug.ProjectId == projectId)
            .ExecuteDelete();
        dbContext.TestStepResults
            .Where(step => testStepResultIds.Contains(step.Id))
            .ExecuteDelete();
        dbContext.TestCaseResults
            .Where(testCase => testCaseResultIds.Contains(testCase.Id))
            .ExecuteDelete();
        dbContext.TestSectionResults
            .Where(section => testSectionResultIds.Contains(section.Id))
            .ExecuteDelete();
        dbContext.TestSessions
            .Where(session => session.ProjectId == projectId)
            .ExecuteDelete();
        dbContext.TestStepTemplates
            .Where(step => testStepTemplateIds.Contains(step.Id))
            .ExecuteDelete();
        dbContext.TestCaseTemplates
            .Where(testCase => testCaseTemplateIds.Contains(testCase.Id))
            .ExecuteDelete();
        dbContext.TemplateSections
            .Where(section => templateSectionIds.Contains(section.Id))
            .ExecuteDelete();
        dbContext.TestSuiteRevisions
            .Where(revision => revisionIds.Contains(revision.Id))
            .ExecuteDelete();
        dbContext.TestSuites
            .Where(suite => suite.ProjectId == projectId)
            .ExecuteDelete();

        dbContext.Projects.Remove(project);
        dbContext.SaveChanges();
    }

    private void DeleteEntitySideData(
        BugTestManagerDbContext dbContext,
        EntityReferenceType entityType,
        IReadOnlyCollection<Guid> entityIds)
    {
        if (entityIds.Count == 0)
        {
            return;
        }

        DeleteAttachmentFiles(dbContext, entityType, entityIds);
        dbContext.DiscussionComments
            .Where(comment => comment.EntityType == entityType && entityIds.Contains(comment.EntityId))
            .ExecuteDelete();
        dbContext.DiscussionReadStates
            .Where(readState => readState.EntityType == entityType && entityIds.Contains(readState.EntityId))
            .ExecuteDelete();
        dbContext.CustomFieldValues
            .Where(value => value.EntityType == entityType && entityIds.Contains(value.EntityId))
            .ExecuteDelete();
    }

    private void DeleteAttachmentFiles(
        BugTestManagerDbContext dbContext,
        EntityReferenceType entityType,
        IReadOnlyCollection<Guid> entityIds)
    {
        var attachments = dbContext.Attachments
            .Where(attachment => attachment.EntityType == entityType && entityIds.Contains(attachment.EntityId))
            .ToList();

        foreach (var attachment in attachments)
        {
            var absolutePath = Path.Combine(attachmentRootPath, attachment.RelativePath);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }

        dbContext.Attachments.RemoveRange(attachments);
        dbContext.SaveChanges();
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
