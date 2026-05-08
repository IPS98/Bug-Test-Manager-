using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Requests;
using BugTestManager.Application.Results;
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

        if (request.RevisionIsRequired && string.IsNullOrWhiteSpace(initialRevisionName))
        {
            throw new ArgumentException("Initial revision name is required when revisions are enabled.", nameof(request));
        }

        using var dbContext = dbContextFactory.CreateDbContext();

        if (dbContext.TestSuites.Any(testSuite => testSuite.Name.ToUpper() == name.ToUpper()))
        {
            throw new InvalidOperationException($"Test suite '{name}' already exists.");
        }

        var testSuiteId = Guid.NewGuid();
        var testSuiteRecord = new TestSuiteRecord
        {
            Id = testSuiteId,
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

    private static string Require(string value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", nameof(value));
        }

        return value.Trim();
    }
}
