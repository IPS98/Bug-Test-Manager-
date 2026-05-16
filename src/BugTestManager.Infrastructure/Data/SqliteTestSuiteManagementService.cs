using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.Requests;
using BugTestManager.Application.Results;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteTestSuiteManagementService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : ITestSuiteManagementService
{
    public CreateTestSuiteResult CreateTestSuite(CreateTestSuiteRequest request)
    {
        var name = Require(request.Name, "Test suite name");
        var description = request.Description.Trim();
        var initialRevisionName = request.InitialRevisionName?.Trim();
        var projectId = ResolveProjectId(request.ProjectId);

        if (request.RevisionIsRequired && string.IsNullOrWhiteSpace(initialRevisionName))
        {
            throw new ArgumentException("Initial revision name is required when revisions are enabled.", nameof(request));
        }

        using var dbContext = dbContextFactory.CreateDbContext();

        if (dbContext.TestSuites.Any(testSuite => testSuite.ProjectId == projectId && testSuite.Name.ToUpper() == name.ToUpper()))
        {
            throw new InvalidOperationException($"Test suite '{name}' already exists.");
        }

        var testSuiteId = Guid.NewGuid();
        var testSuiteRecord = new TestSuiteRecord
        {
            Id = testSuiteId,
            ProjectId = projectId,
            Name = name,
            Description = description,
            RevisionIsRequired = request.RevisionIsRequired,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.TestSuites.Add(testSuiteRecord);

        Guid? revisionId = null;
        if (request.RevisionIsRequired)
        {
            revisionId = Guid.NewGuid();
            dbContext.TestSuiteRevisions.Add(new TestSuiteRevisionRecord
            {
                Id = revisionId.Value,
                TestSuiteId = testSuiteId,
                Name = initialRevisionName!,
                SortOrder = 1
            });
        }

        dbContext.SaveChanges();

        return new CreateTestSuiteResult(testSuiteId, revisionId);
    }

    public Guid CreateRevision(CreateTestSuiteRevisionRequest request)
    {
        var name = Require(request.Name, "Revision name");

        using var dbContext = dbContextFactory.CreateDbContext();
        var testSuite = dbContext.TestSuites
            .Include(suite => suite.Revisions)
            .Include(suite => suite.Sections)
            .ThenInclude(section => section.TestCases)
            .ThenInclude(testCase => testCase.Steps)
            .SingleOrDefault(suite => suite.Id == request.TestSuiteId)
            ?? throw new InvalidOperationException("Selected test suite was not found.");

        if (testSuite.Revisions.Any(revision => revision.Name.ToUpper() == name.ToUpper()))
        {
            throw new InvalidOperationException($"Revision '{name}' already exists.");
        }

        var revisionId = Guid.NewGuid();
        var nextSortOrder = testSuite.Revisions
            .Select(revision => revision.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        dbContext.TestSuiteRevisions.Add(new TestSuiteRevisionRecord
        {
            Id = revisionId,
            TestSuiteId = testSuite.Id,
            Name = name,
            SortOrder = nextSortOrder
        });

        if (request.SourceRevisionId is { } sourceRevisionId && sourceRevisionId != Guid.Empty)
        {
            var sourceRevisionExists = testSuite.Revisions.Any(revision => revision.Id == sourceRevisionId);
            if (!sourceRevisionExists)
            {
                throw new InvalidOperationException("Source revision was not found.");
            }

            CopyRevisionSections(dbContext, testSuite.Sections
                .Where(section => section.TestSuiteRevisionId == sourceRevisionId)
                .OrderBy(section => section.SortOrder), testSuite.Id, revisionId);
        }

        dbContext.SaveChanges();
        return revisionId;
    }

    public Guid CreateSection(CreateTemplateSectionRequest request)
    {
        var name = Require(request.Name, "Section name");
        var category = request.Category.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var testSuite = dbContext.TestSuites
            .Include(suite => suite.Sections)
            .SingleOrDefault(suite => suite.Id == request.TestSuiteId)
            ?? throw new InvalidOperationException("Selected test suite was not found.");

        if (testSuite.RevisionIsRequired && request.TestSuiteRevisionId is null)
        {
            throw new InvalidOperationException("A revision must be selected before adding a section.");
        }

        if (request.TestSuiteRevisionId is not null)
        {
            var revisionExists = dbContext.TestSuiteRevisions.Any(revision =>
                revision.Id == request.TestSuiteRevisionId && revision.TestSuiteId == request.TestSuiteId);

            if (!revisionExists)
            {
                throw new InvalidOperationException("Selected revision was not found.");
            }
        }

        var nextSortOrder = testSuite.Sections
            .Where(section => section.TestSuiteRevisionId == request.TestSuiteRevisionId)
            .Select(section => section.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var sectionId = Guid.NewGuid();
        dbContext.TemplateSections.Add(new TemplateSectionRecord
        {
            Id = sectionId,
            TestSuiteId = request.TestSuiteId,
            TestSuiteRevisionId = request.TestSuiteRevisionId,
            Name = name,
            Category = category,
            SortOrder = nextSortOrder
        });

        dbContext.SaveChanges();

        return sectionId;
    }

    public Guid CreateTestCase(CreateTestCaseTemplateRequest request)
    {
        var title = Require(request.Title, "Test case title");
        var expectedResult = request.ExpectedResult.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var section = dbContext.TemplateSections
            .Include(templateSection => templateSection.TestCases)
            .SingleOrDefault(templateSection => templateSection.Id == request.TemplateSectionId)
            ?? throw new InvalidOperationException("Selected section was not found.");

        var nextSortOrder = section.TestCases
            .Select(testCase => testCase.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var testCaseId = Guid.NewGuid();
        dbContext.TestCaseTemplates.Add(new TestCaseTemplateRecord
        {
            Id = testCaseId,
            TemplateSectionId = section.Id,
            Title = title,
            ExpectedResult = expectedResult,
            SortOrder = nextSortOrder
        });

        dbContext.SaveChanges();

        return testCaseId;
    }

    public Guid CreateTestStep(CreateTestStepTemplateRequest request)
    {
        var stepText = Require(request.StepText, "Step text");
        var expectedResult = request.ExpectedResult.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var testCase = dbContext.TestCaseTemplates
            .Include(template => template.Steps)
            .SingleOrDefault(template => template.Id == request.TestCaseTemplateId)
            ?? throw new InvalidOperationException("Selected test case was not found.");

        var nextSortOrder = testCase.Steps
            .Select(step => step.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var testStepId = Guid.NewGuid();
        dbContext.TestStepTemplates.Add(new TestStepTemplateRecord
        {
            Id = testStepId,
            TestCaseTemplateId = testCase.Id,
            StepText = stepText,
            ExpectedResult = expectedResult,
            SortOrder = nextSortOrder
        });

        dbContext.SaveChanges();

        return testStepId;
    }

    public void UpdateTestSuite(UpdateTestSuiteRequest request)
    {
        var name = Require(request.Name, "Test suite name");
        var description = request.Description.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var testSuite = dbContext.TestSuites
            .Include(suite => suite.Revisions)
            .Include(suite => suite.Sections)
            .SingleOrDefault(suite => suite.Id == request.TestSuiteId)
            ?? throw new InvalidOperationException("Selected test suite was not found.");

        if (dbContext.TestSuites.Any(suite =>
                suite.Id != request.TestSuiteId
                && suite.ProjectId == testSuite.ProjectId
                && suite.Name.ToUpper() == name.ToUpper()))
        {
            throw new InvalidOperationException($"Test suite '{name}' already exists.");
        }

        testSuite.Name = name;
        testSuite.Description = description;
        testSuite.RevisionIsRequired = request.RevisionIsRequired;

        if (request.RevisionIsRequired)
        {
            EnsureRevisionExistsForRequiredSuite(dbContext, testSuite, request.InitialRevisionName);
        }

        dbContext.SaveChanges();
    }

    public void UpdateRevision(UpdateTestSuiteRevisionRequest request)
    {
        var name = Require(request.Name, "Revision name");

        using var dbContext = dbContextFactory.CreateDbContext();
        var revision = dbContext.TestSuiteRevisions.SingleOrDefault(item => item.Id == request.TestSuiteRevisionId)
            ?? throw new InvalidOperationException("Selected revision was not found.");

        var duplicateExists = dbContext.TestSuiteRevisions.Any(item =>
            item.Id != request.TestSuiteRevisionId
            && item.TestSuiteId == revision.TestSuiteId
            && item.Name.ToUpper() == name.ToUpper());
        if (duplicateExists)
        {
            throw new InvalidOperationException($"Revision '{name}' already exists.");
        }

        revision.Name = name;
        dbContext.SaveChanges();
    }

    public void UpdateSection(UpdateTemplateSectionRequest request)
    {
        var name = Require(request.Name, "Section name");
        var category = request.Category.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var section = dbContext.TemplateSections.SingleOrDefault(templateSection => templateSection.Id == request.SectionId)
            ?? throw new InvalidOperationException("Selected section was not found.");

        section.Name = name;
        section.Category = category;
        dbContext.SaveChanges();
    }

    public void UpdateTestCase(UpdateTestCaseTemplateRequest request)
    {
        var title = Require(request.Title, "Test case title");
        var expectedResult = request.ExpectedResult.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var testCase = dbContext.TestCaseTemplates.SingleOrDefault(template => template.Id == request.TestCaseId)
            ?? throw new InvalidOperationException("Selected test case was not found.");

        testCase.Title = title;
        testCase.ExpectedResult = expectedResult;
        dbContext.SaveChanges();
    }

    public void UpdateTestStep(UpdateTestStepTemplateRequest request)
    {
        var stepText = Require(request.StepText, "Step text");
        var expectedResult = request.ExpectedResult.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        var step = dbContext.TestStepTemplates.SingleOrDefault(templateStep => templateStep.Id == request.TestStepId)
            ?? throw new InvalidOperationException("Selected step was not found.");

        step.StepText = stepText;
        step.ExpectedResult = expectedResult;
        dbContext.SaveChanges();
    }

    public void DeleteTestSuite(Guid testSuiteId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var testSuite = dbContext.TestSuites.SingleOrDefault(suite => suite.Id == testSuiteId)
            ?? throw new InvalidOperationException("Selected test suite was not found.");

        dbContext.TestSuites.Remove(testSuite);
        dbContext.SaveChanges();
    }

    public void DeleteRevision(Guid testSuiteRevisionId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var revision = dbContext.TestSuiteRevisions.SingleOrDefault(item => item.Id == testSuiteRevisionId)
            ?? throw new InvalidOperationException("Selected revision was not found.");

        var sectionIds = dbContext.TemplateSections
            .Where(section => section.TestSuiteRevisionId == testSuiteRevisionId)
            .Select(section => section.Id)
            .ToList();
        var testCaseIds = dbContext.TestCaseTemplates
            .Where(testCase => sectionIds.Contains(testCase.TemplateSectionId))
            .Select(testCase => testCase.Id)
            .ToList();
        var testStepIds = dbContext.TestStepTemplates
            .Where(step => testCaseIds.Contains(step.TestCaseTemplateId))
            .Select(step => step.Id)
            .ToList();

        DeleteTemplateSideData(dbContext, EntityReferenceType.TestSuiteRevision, [testSuiteRevisionId]);
        DeleteTemplateSideData(dbContext, EntityReferenceType.TemplateSection, sectionIds);
        DeleteTemplateSideData(dbContext, EntityReferenceType.TestCaseTemplate, testCaseIds);
        DeleteTemplateSideData(dbContext, EntityReferenceType.TestStepTemplate, testStepIds);
        DeleteFieldScopesForTemplateItems(dbContext, CustomFieldScopeType.TemplateSection, sectionIds);
        DeleteFieldScopesForTemplateItems(dbContext, CustomFieldScopeType.TestCaseTemplate, testCaseIds);

        dbContext.TestSuiteRevisions.Remove(revision);
        dbContext.SaveChanges();
    }

    public void DeleteSection(Guid sectionId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var section = dbContext.TemplateSections.SingleOrDefault(templateSection => templateSection.Id == sectionId)
            ?? throw new InvalidOperationException("Selected section was not found.");

        dbContext.TemplateSections.Remove(section);
        dbContext.SaveChanges();
    }

    public void DeleteTestCase(Guid testCaseId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var testCase = dbContext.TestCaseTemplates.SingleOrDefault(template => template.Id == testCaseId)
            ?? throw new InvalidOperationException("Selected test case was not found.");

        dbContext.TestCaseTemplates.Remove(testCase);
        dbContext.SaveChanges();
    }

    public void DeleteTestStep(Guid testStepId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var step = dbContext.TestStepTemplates.SingleOrDefault(templateStep => templateStep.Id == testStepId)
            ?? throw new InvalidOperationException("Selected step was not found.");

        dbContext.TestStepTemplates.Remove(step);
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

    private static Guid ResolveProjectId(Guid? projectId)
    {
        return projectId ?? ProjectDefaults.DefaultProjectId;
    }

    private static void EnsureRevisionExistsForRequiredSuite(
        BugTestManagerDbContext dbContext,
        TestSuiteRecord testSuite,
        string? initialRevisionName)
    {
        if (testSuite.Revisions.Count > 0)
        {
            var firstRevisionId = testSuite.Revisions
                .OrderBy(revision => revision.SortOrder)
                .First()
                .Id;

            foreach (var section in testSuite.Sections.Where(section => section.TestSuiteRevisionId is null))
            {
                section.TestSuiteRevisionId = firstRevisionId;
            }

            return;
        }

        var name = Require(initialRevisionName ?? string.Empty, "Initial revision name");
        var revisionId = Guid.NewGuid();
        dbContext.TestSuiteRevisions.Add(new TestSuiteRevisionRecord
        {
            Id = revisionId,
            TestSuiteId = testSuite.Id,
            Name = name,
            SortOrder = 1
        });

        foreach (var section in testSuite.Sections)
        {
            section.TestSuiteRevisionId = revisionId;
        }
    }

    private static void CopyRevisionSections(
        BugTestManagerDbContext dbContext,
        IEnumerable<TemplateSectionRecord> sourceSections,
        Guid testSuiteId,
        Guid targetRevisionId)
    {
        foreach (var sourceSection in sourceSections)
        {
            var sectionId = Guid.NewGuid();
            var section = new TemplateSectionRecord
            {
                Id = sectionId,
                TestSuiteId = testSuiteId,
                TestSuiteRevisionId = targetRevisionId,
                Name = sourceSection.Name,
                Category = sourceSection.Category,
                SortOrder = sourceSection.SortOrder
            };

            foreach (var sourceTestCase in sourceSection.TestCases.OrderBy(testCase => testCase.SortOrder))
            {
                var testCaseId = Guid.NewGuid();
                var testCase = new TestCaseTemplateRecord
                {
                    Id = testCaseId,
                    TemplateSectionId = sectionId,
                    Title = sourceTestCase.Title,
                    ExpectedResult = sourceTestCase.ExpectedResult,
                    SortOrder = sourceTestCase.SortOrder
                };

                foreach (var sourceStep in sourceTestCase.Steps.OrderBy(step => step.SortOrder))
                {
                    testCase.Steps.Add(new TestStepTemplateRecord
                    {
                        Id = Guid.NewGuid(),
                        TestCaseTemplateId = testCaseId,
                        StepText = sourceStep.StepText,
                        ExpectedResult = sourceStep.ExpectedResult,
                        SortOrder = sourceStep.SortOrder
                    });
                }

                section.TestCases.Add(testCase);
            }

            dbContext.TemplateSections.Add(section);
        }
    }

    private static void DeleteTemplateSideData(
        BugTestManagerDbContext dbContext,
        EntityReferenceType entityType,
        IReadOnlyCollection<Guid> entityIds)
    {
        if (entityIds.Count == 0)
        {
            return;
        }

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

    private static void DeleteFieldScopesForTemplateItems(
        BugTestManagerDbContext dbContext,
        CustomFieldScopeType scopeType,
        IReadOnlyCollection<Guid> entityIds)
    {
        if (entityIds.Count == 0)
        {
            return;
        }

        dbContext.CustomFieldDefinitionScopes
            .Where(scope =>
                scope.ScopeType == scopeType
                && scope.ScopeEntityId.HasValue
                && entityIds.Contains(scope.ScopeEntityId.Value))
            .ExecuteDelete();
    }
}
