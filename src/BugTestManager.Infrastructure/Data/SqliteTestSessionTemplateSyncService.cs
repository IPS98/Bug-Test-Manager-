using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Application.Results;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteTestSessionTemplateSyncService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : ITestSessionTemplateSyncService
{
    public TestSessionTemplateSyncPreview GetPreview(Guid testSessionId, Guid? projectId = null)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var resolvedProjectId = ResolveProjectId(projectId);
        var session = LoadSession(dbContext, testSessionId, resolvedProjectId, asTracking: false);
        var canUpdateOriginal = CanUpdateOriginalTemplate(dbContext, session);
        var counts = CountManualStructure(session);
        var notes = BuildPreviewNotes(session, canUpdateOriginal, counts);

        return new TestSessionTemplateSyncPreview(
            session.Id,
            session.Name,
            canUpdateOriginal ? session.TestSuiteId : null,
            canUpdateOriginal ? session.TestSuiteName : "No original template",
            session.TestSuiteRevisionName,
            canUpdateOriginal,
            session.Sections.Count,
            session.Sections.Sum(section => section.TestCases.Count),
            session.Sections.Sum(section => section.TestCases.Sum(testCase => testCase.Steps.Count)),
            counts.SectionCount,
            counts.TestCaseCount,
            counts.CheckCount,
            notes);
    }

    public TestSessionTemplateSyncResult UpdateOriginalTemplate(UpdateTemplateFromSessionRequest request)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        using var transaction = dbContext.Database.BeginTransaction();
        var resolvedProjectId = ResolveProjectId(request.ProjectId);
        var session = LoadSession(dbContext, request.TestSessionId, resolvedProjectId, asTracking: true);
        var testSuite = dbContext.TestSuites
            .SingleOrDefault(suite => suite.Id == session.TestSuiteId && suite.ProjectId == resolvedProjectId)
            ?? throw new InvalidOperationException("Original template was not found.");

        if (session.IsManual || session.TestSuiteId == Guid.Empty)
        {
            throw new InvalidOperationException("This session was created without a template. Create a new template instead.");
        }

        if (testSuite.RevisionIsRequired && session.TestSuiteRevisionId is null)
        {
            throw new InvalidOperationException("Original template requires a revision, but this session has no revision.");
        }

        var addedSections = 0;
        var addedTestCases = 0;
        var addedChecks = 0;
        var targetRevisionId = testSuite.RevisionIsRequired ? session.TestSuiteRevisionId : null;
        var nextSectionSortOrders = new Dictionary<Guid, int>();
        var nextTestCaseSortOrders = new Dictionary<Guid, int>();
        var nextCheckSortOrders = new Dictionary<Guid, int>();

        foreach (var section in session.Sections.OrderBy(section => section.SortOrder))
        {
            var targetSectionId = ResolveOrCreateSection(
                dbContext,
                section,
                testSuite.Id,
                targetRevisionId,
                nextSectionSortOrders,
                ref addedSections);

            foreach (var testCase in section.TestCases.OrderBy(testCase => testCase.SortOrder))
            {
                var targetTestCaseId = ResolveOrCreateTestCase(
                    dbContext,
                    testCase,
                    targetSectionId,
                    nextTestCaseSortOrders,
                    ref addedTestCases);

                foreach (var check in testCase.Steps.OrderBy(step => step.SortOrder))
                {
                    ResolveOrCreateCheck(
                        dbContext,
                        check,
                        targetTestCaseId,
                        nextCheckSortOrders,
                        ref addedChecks);
                }
            }
        }

        dbContext.SaveChanges();
        transaction.Commit();

        return new TestSessionTemplateSyncResult(
            testSuite.Id,
            testSuite.Name,
            addedSections,
            addedTestCases,
            addedChecks);
    }

    public TestSessionTemplateSyncResult CreateTemplateFromSession(CreateTemplateFromSessionRequest request)
    {
        var templateName = Require(request.TemplateName, "Template name");
        var description = request.Description.Trim();

        using var dbContext = dbContextFactory.CreateDbContext();
        using var transaction = dbContext.Database.BeginTransaction();
        var resolvedProjectId = ResolveProjectId(request.ProjectId);
        var session = LoadSession(dbContext, request.TestSessionId, resolvedProjectId, asTracking: false);

        EnsureUniqueTemplateName(dbContext, resolvedProjectId, templateName);

        var revisionIsRequired = session.TestSuiteRevisionId.HasValue
            && !string.IsNullOrWhiteSpace(session.TestSuiteRevisionName);
        var testSuiteId = Guid.NewGuid();
        var revisionId = revisionIsRequired ? Guid.NewGuid() : (Guid?)null;
        var testSuite = new TestSuiteRecord
        {
            Id = testSuiteId,
            ProjectId = resolvedProjectId,
            Name = templateName,
            Description = string.IsNullOrWhiteSpace(description)
                ? $"Created from test session '{session.Name}'."
                : description,
            RevisionIsRequired = revisionIsRequired,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.TestSuites.Add(testSuite);

        if (revisionIsRequired)
        {
            dbContext.TestSuiteRevisions.Add(new TestSuiteRevisionRecord
            {
                Id = revisionId!.Value,
                TestSuiteId = testSuiteId,
                Name = session.TestSuiteRevisionName!,
                SortOrder = 1
            });
        }

        var addedSections = 0;
        var addedTestCases = 0;
        var addedChecks = 0;

        foreach (var sourceSection in session.Sections.OrderBy(section => section.SortOrder))
        {
            var sectionId = Guid.NewGuid();
            var section = new TemplateSectionRecord
            {
                Id = sectionId,
                TestSuiteId = testSuiteId,
                TestSuiteRevisionId = revisionId,
                Name = sourceSection.Name,
                Category = sourceSection.Category,
                SortOrder = sourceSection.SortOrder
            };
            addedSections++;

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
                addedTestCases++;

                foreach (var sourceCheck in sourceTestCase.Steps.OrderBy(step => step.SortOrder))
                {
                    testCase.Steps.Add(new TestStepTemplateRecord
                    {
                        Id = Guid.NewGuid(),
                        TestCaseTemplateId = testCaseId,
                        StepText = sourceCheck.StepText,
                        ExpectedResult = sourceCheck.ExpectedResult,
                        SortOrder = sourceCheck.SortOrder
                    });
                    addedChecks++;
                }

                section.TestCases.Add(testCase);
            }

            dbContext.TemplateSections.Add(section);
        }

        dbContext.SaveChanges();
        transaction.Commit();

        return new TestSessionTemplateSyncResult(
            testSuiteId,
            templateName,
            addedSections,
            addedTestCases,
            addedChecks);
    }

    private static TestSessionRecord LoadSession(
        BugTestManagerDbContext dbContext,
        Guid testSessionId,
        Guid projectId,
        bool asTracking)
    {
        var query = dbContext.TestSessions
            .Include(session => session.Sections)
            .ThenInclude(section => section.TestCases)
            .ThenInclude(testCase => testCase.Steps)
            .Where(session => session.Id == testSessionId && session.ProjectId == projectId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefault()
            ?? throw new InvalidOperationException("Selected test session was not found.");
    }

    private static bool CanUpdateOriginalTemplate(BugTestManagerDbContext dbContext, TestSessionRecord session)
    {
        return !session.IsManual
            && session.TestSuiteId != Guid.Empty
            && dbContext.TestSuites.AsNoTracking().Any(suite =>
                suite.Id == session.TestSuiteId
                && suite.ProjectId == session.ProjectId);
    }

    private static (int SectionCount, int TestCaseCount, int CheckCount) CountManualStructure(TestSessionRecord session)
    {
        var manualSections = session.Sections.Count(section => section.TemplateSectionId == Guid.Empty);
        var manualTestCases = session.Sections
            .SelectMany(section => section.TestCases)
            .Count(testCase => testCase.TestCaseTemplateId == Guid.Empty);
        var manualChecks = session.Sections
            .SelectMany(section => section.TestCases)
            .SelectMany(testCase => testCase.Steps)
            .Count(check => check.TestStepTemplateId == Guid.Empty);

        return (manualSections, manualTestCases, manualChecks);
    }

    private static IReadOnlyList<string> BuildPreviewNotes(
        TestSessionRecord session,
        bool canUpdateOriginal,
        (int SectionCount, int TestCaseCount, int CheckCount) counts)
    {
        var notes = new List<string>
        {
            "Only structure is saved to templates: sections, test cases, and checks.",
            "Statuses, comments, attachments, linked bugs, and discussions stay only in this test session."
        };

        if (!canUpdateOriginal)
        {
            notes.Add("This session has no original template, so it can only create a new template.");
        }
        else if (counts.SectionCount == 0 && counts.TestCaseCount == 0 && counts.CheckCount == 0)
        {
            notes.Add("No manual structure changes were found for updating the original template.");
        }
        else
        {
            notes.Add("Updating the original template will append only manual items that are not linked to template items yet.");
        }

        if (session.TestSuiteRevisionId.HasValue && !string.IsNullOrWhiteSpace(session.TestSuiteRevisionName))
        {
            notes.Add($"The new template option will keep revision '{session.TestSuiteRevisionName}'.");
        }

        return notes;
    }

    private static Guid ResolveOrCreateSection(
        BugTestManagerDbContext dbContext,
        TestSectionResultRecord sourceSection,
        Guid testSuiteId,
        Guid? targetRevisionId,
        Dictionary<Guid, int> nextSectionSortOrders,
        ref int addedSections)
    {
        if (sourceSection.TemplateSectionId != Guid.Empty
            && dbContext.TemplateSections.Any(section =>
                section.Id == sourceSection.TemplateSectionId
                && section.TestSuiteId == testSuiteId))
        {
            return sourceSection.TemplateSectionId;
        }

        var sectionId = Guid.NewGuid();
        var section = new TemplateSectionRecord
        {
            Id = sectionId,
            TestSuiteId = testSuiteId,
            TestSuiteRevisionId = targetRevisionId,
            Name = sourceSection.Name,
            Category = sourceSection.Category,
            SortOrder = GetNextSectionSortOrder(dbContext, testSuiteId, targetRevisionId, nextSectionSortOrders)
        };

        dbContext.TemplateSections.Add(section);
        sourceSection.TemplateSectionId = sectionId;
        addedSections++;

        return sectionId;
    }

    private static Guid ResolveOrCreateTestCase(
        BugTestManagerDbContext dbContext,
        TestCaseResultRecord sourceTestCase,
        Guid targetSectionId,
        Dictionary<Guid, int> nextTestCaseSortOrders,
        ref int addedTestCases)
    {
        if (sourceTestCase.TestCaseTemplateId != Guid.Empty
            && dbContext.TestCaseTemplates.Any(testCase =>
                testCase.Id == sourceTestCase.TestCaseTemplateId
                && testCase.TemplateSectionId == targetSectionId))
        {
            return sourceTestCase.TestCaseTemplateId;
        }

        var testCaseId = Guid.NewGuid();
        var testCase = new TestCaseTemplateRecord
        {
            Id = testCaseId,
            TemplateSectionId = targetSectionId,
            Title = sourceTestCase.Title,
            ExpectedResult = sourceTestCase.ExpectedResult,
            SortOrder = GetNextTestCaseSortOrder(dbContext, targetSectionId, nextTestCaseSortOrders)
        };

        dbContext.TestCaseTemplates.Add(testCase);
        sourceTestCase.TestCaseTemplateId = testCaseId;
        addedTestCases++;

        return testCaseId;
    }

    private static void ResolveOrCreateCheck(
        BugTestManagerDbContext dbContext,
        TestStepResultRecord sourceCheck,
        Guid targetTestCaseId,
        Dictionary<Guid, int> nextCheckSortOrders,
        ref int addedChecks)
    {
        if (sourceCheck.TestStepTemplateId != Guid.Empty
            && dbContext.TestStepTemplates.Any(check =>
                check.Id == sourceCheck.TestStepTemplateId
                && check.TestCaseTemplateId == targetTestCaseId))
        {
            return;
        }

        var checkId = Guid.NewGuid();
        dbContext.TestStepTemplates.Add(new TestStepTemplateRecord
        {
            Id = checkId,
            TestCaseTemplateId = targetTestCaseId,
            StepText = sourceCheck.StepText,
            ExpectedResult = sourceCheck.ExpectedResult,
            SortOrder = GetNextCheckSortOrder(dbContext, targetTestCaseId, nextCheckSortOrders)
        });
        sourceCheck.TestStepTemplateId = checkId;
        addedChecks++;
    }

    private static int GetNextSectionSortOrder(
        BugTestManagerDbContext dbContext,
        Guid testSuiteId,
        Guid? revisionId,
        Dictionary<Guid, int> nextSectionSortOrders)
    {
        var key = revisionId ?? Guid.Empty;
        if (!nextSectionSortOrders.TryGetValue(key, out var nextSortOrder))
        {
            nextSortOrder = dbContext.TemplateSections
                .Where(section => section.TestSuiteId == testSuiteId && section.TestSuiteRevisionId == revisionId)
                .Select(section => (int?)section.SortOrder)
                .Max().GetValueOrDefault() + 1;
        }

        nextSectionSortOrders[key] = nextSortOrder + 1;
        return nextSortOrder;
    }

    private static int GetNextTestCaseSortOrder(
        BugTestManagerDbContext dbContext,
        Guid templateSectionId,
        Dictionary<Guid, int> nextTestCaseSortOrders)
    {
        if (!nextTestCaseSortOrders.TryGetValue(templateSectionId, out var nextSortOrder))
        {
            nextSortOrder = dbContext.TestCaseTemplates
                .Where(testCase => testCase.TemplateSectionId == templateSectionId)
                .Select(testCase => (int?)testCase.SortOrder)
                .Max().GetValueOrDefault() + 1;
        }

        nextTestCaseSortOrders[templateSectionId] = nextSortOrder + 1;
        return nextSortOrder;
    }

    private static int GetNextCheckSortOrder(
        BugTestManagerDbContext dbContext,
        Guid testCaseTemplateId,
        Dictionary<Guid, int> nextCheckSortOrders)
    {
        if (!nextCheckSortOrders.TryGetValue(testCaseTemplateId, out var nextSortOrder))
        {
            nextSortOrder = dbContext.TestStepTemplates
                .Where(check => check.TestCaseTemplateId == testCaseTemplateId)
                .Select(check => (int?)check.SortOrder)
                .Max().GetValueOrDefault() + 1;
        }

        nextCheckSortOrders[testCaseTemplateId] = nextSortOrder + 1;
        return nextSortOrder;
    }

    private static void EnsureUniqueTemplateName(
        BugTestManagerDbContext dbContext,
        Guid projectId,
        string templateName)
    {
        var normalizedName = Normalize(templateName);
        var duplicateExists = dbContext.TestSuites
            .AsNoTracking()
            .Where(suite => suite.ProjectId == projectId)
            .Select(suite => suite.Name)
            .ToList()
            .Any(existingName => Normalize(existingName) == normalizedName);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Test suite '{templateName}' already exists.");
        }
    }

    private static string Require(string value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", nameof(value));
        }

        return value.Trim();
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static Guid ResolveProjectId(Guid? projectId)
    {
        return projectId ?? ProjectDefaults.DefaultProjectId;
    }
}
