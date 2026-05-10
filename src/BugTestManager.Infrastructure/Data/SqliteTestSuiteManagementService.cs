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
        var testSuite = dbContext.TestSuites.SingleOrDefault(suite => suite.Id == request.TestSuiteId)
            ?? throw new InvalidOperationException("Selected test suite was not found.");

        if (dbContext.TestSuites.Any(suite => suite.Id != request.TestSuiteId && suite.Name.ToUpper() == name.ToUpper()))
        {
            throw new InvalidOperationException($"Test suite '{name}' already exists.");
        }

        testSuite.Name = name;
        testSuite.Description = description;
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
}
