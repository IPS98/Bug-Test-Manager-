using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteTestSuiteCatalogService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory) : ITestSuiteCatalogService
{
    public IReadOnlyList<TestSuiteCatalogItem> GetCatalog()
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        var testSuites = dbContext.TestSuites
            .AsNoTracking()
            .Include(testSuite => testSuite.Revisions)
            .Include(testSuite => testSuite.Sections)
                .ThenInclude(section => section.TestCases)
                    .ThenInclude(testCase => testCase.Steps)
            .OrderBy(testSuite => testSuite.Name)
            .ToList();

        return testSuites.Select(MapTestSuite).ToList();
    }

    private static TestSuiteCatalogItem MapTestSuite(TestSuiteRecord testSuite)
    {
        var revisions = testSuite.RevisionIsRequired
            ? testSuite.Revisions
                .OrderBy(revision => revision.SortOrder)
                .Select(revision => MapRevision(revision, testSuite.Sections.Where(section => section.TestSuiteRevisionId == revision.Id)))
                .ToList()
            :
            [
                MapOptionalRevision(testSuite.Sections)
            ];

        return new TestSuiteCatalogItem(
            testSuite.Id,
            testSuite.Name,
            testSuite.Description,
            testSuite.RevisionIsRequired,
            revisions);
    }

    private static TestSuiteRevisionCatalogItem MapRevision(
        TestSuiteRevisionRecord revision,
        IEnumerable<TemplateSectionRecord> sections)
    {
        return new TestSuiteRevisionCatalogItem(
            revision.Id,
            revision.Name,
            revision.EffectiveDate,
            MapSections(sections));
    }

    private static TestSuiteRevisionCatalogItem MapOptionalRevision(IEnumerable<TemplateSectionRecord> sections)
    {
        return new TestSuiteRevisionCatalogItem(
            Guid.Empty,
            "No revision",
            null,
            MapSections(sections));
    }

    private static IReadOnlyList<TemplateSectionCatalogItem> MapSections(IEnumerable<TemplateSectionRecord> sections)
    {
        return sections
            .OrderBy(section => section.SortOrder)
            .Select(section =>
                new TemplateSectionCatalogItem(
                    section.Id,
                    section.Name,
                    section.Category,
                    section.SortOrder,
                    section.TestCases
                        .OrderBy(testCase => testCase.SortOrder)
                        .Select(testCase =>
                            new TestCaseTemplateCatalogItem(
                                testCase.Id,
                                testCase.Title,
                                testCase.ExpectedResult,
                                testCase.SortOrder,
                                testCase.Steps
                                    .OrderBy(step => step.SortOrder)
                                    .Select(step =>
                                        new TestStepTemplateCatalogItem(
                                            step.Id,
                                            step.StepText,
                                            step.ExpectedResult,
                                            step.SortOrder))
                                    .ToList()))
                        .ToList()))
            .ToList();
    }
}
