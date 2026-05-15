using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteTestSessionService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : ITestSessionService
{
    public IReadOnlyList<TestSessionSummaryItem> GetSessions()
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        return dbContext.TestSessions
            .AsNoTracking()
            .Include(session => session.Sections)
            .ThenInclude(section => section.TestCases)
            .ThenInclude(testCase => testCase.Steps)
            .ToList()
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new TestSessionSummaryItem(
                session.Id,
                session.Name,
                session.TestSuiteName,
                session.TestSuiteRevisionName,
                session.TestedVersion,
                session.BuildNumber,
                session.CreatedBy,
                session.CreatedAt,
                session.Sections.Count,
                session.Sections.Sum(section => section.TestCases.Count),
                session.Sections.Sum(section => section.TestCases.Sum(testCase => testCase.Steps.Count))))
            .ToList();
    }

    public TestSessionDetailsItem GetSession(Guid testSessionId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var session = dbContext.TestSessions
            .AsNoTracking()
            .Include(item => item.Sections)
            .ThenInclude(section => section.TestCases)
            .ThenInclude(testCase => testCase.Steps)
            .SingleOrDefault(item => item.Id == testSessionId)
            ?? throw new InvalidOperationException("Selected test session was not found.");

        return new TestSessionDetailsItem(
            session.Id,
            session.Name,
            session.TestSuiteName,
            session.TestSuiteRevisionName,
            session.TestedVersion,
            session.BuildNumber,
            session.Notes,
            session.CreatedBy,
            session.CreatedAt,
            session.Sections
                .OrderBy(section => section.SortOrder)
                .Select(section => new TestSectionResultItem(
                    section.Id,
                    section.Name,
                    section.Category,
                    section.SortOrder,
                    section.TestCases
                        .OrderBy(testCase => testCase.SortOrder)
                        .Select(testCase => new TestCaseResultItem(
                            testCase.Id,
                            testCase.Title,
                            testCase.ExpectedResult,
                            testCase.SortOrder,
                            testCase.Status,
                            testCase.Comment,
                            testCase.Steps
                                .OrderBy(step => step.SortOrder)
                                .Select(step => new TestStepResultItem(
                                    step.Id,
                                    step.StepText,
                                    step.ExpectedResult,
                                    step.SortOrder,
                                    step.Status,
                                    step.Comment))
                                .ToList()))
                        .ToList()))
                .ToList());
    }

    public Guid CreateSession(CreateTestSessionRequest request)
    {
        var name = Require(request.Name, "Session name");
        var createdBy = Require(request.CreatedBy, "Created by");

        using var dbContext = dbContextFactory.CreateDbContext();
        var testSuite = dbContext.TestSuites
            .AsNoTracking()
            .Include(suite => suite.Revisions)
            .Include(suite => suite.Sections)
            .ThenInclude(section => section.TestCases)
            .ThenInclude(testCase => testCase.Steps)
            .SingleOrDefault(suite => suite.Id == request.TestSuiteId)
            ?? throw new InvalidOperationException("Selected test suite was not found.");

        if (testSuite.RevisionIsRequired && request.TestSuiteRevisionId is null)
        {
            throw new InvalidOperationException("A revision must be selected before creating a session.");
        }

        var revision = request.TestSuiteRevisionId is null
            ? null
            : testSuite.Revisions.SingleOrDefault(item => item.Id == request.TestSuiteRevisionId)
                ?? throw new InvalidOperationException("Selected revision was not found.");

        var sections = testSuite.Sections
            .Where(section => section.TestSuiteRevisionId == request.TestSuiteRevisionId)
            .OrderBy(section => section.SortOrder)
            .ToList();

        var sessionId = Guid.NewGuid();
        var session = new TestSessionRecord
        {
            Id = sessionId,
            Name = name,
            TestSuiteId = testSuite.Id,
            TestSuiteRevisionId = request.TestSuiteRevisionId,
            IsManual = false,
            TestSuiteName = testSuite.Name,
            TestSuiteRevisionName = revision?.Name,
            TestedVersion = request.TestedVersion.Trim(),
            BuildNumber = request.BuildNumber.Trim(),
            Notes = request.Notes.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var section in sections)
        {
            var sectionResult = new TestSectionResultRecord
            {
                Id = Guid.NewGuid(),
                TestSessionId = sessionId,
                TemplateSectionId = section.Id,
                Name = section.Name,
                Category = section.Category,
                SortOrder = section.SortOrder
            };

            foreach (var testCase in section.TestCases.OrderBy(testCase => testCase.SortOrder))
            {
                var testCaseResult = new TestCaseResultRecord
                {
                    Id = Guid.NewGuid(),
                    TestCaseTemplateId = testCase.Id,
                    Title = testCase.Title,
                    ExpectedResult = testCase.ExpectedResult,
                    SortOrder = testCase.SortOrder,
                    Status = TestResultStatus.NotTested,
                    Comment = string.Empty
                };

                foreach (var step in testCase.Steps.OrderBy(step => step.SortOrder))
                {
                    testCaseResult.Steps.Add(new TestStepResultRecord
                    {
                        Id = Guid.NewGuid(),
                        TestStepTemplateId = step.Id,
                        StepText = step.StepText,
                        ExpectedResult = step.ExpectedResult,
                        SortOrder = step.SortOrder,
                        Status = TestResultStatus.NotTested,
                        Comment = string.Empty
                    });
                }

                sectionResult.TestCases.Add(testCaseResult);
            }

            session.Sections.Add(sectionResult);
        }

        dbContext.TestSessions.Add(session);
        dbContext.SaveChanges();

        return sessionId;
    }

    public Guid CreateManualSession(CreateManualTestSessionRequest request)
    {
        var name = Require(request.Name, "Session name");
        var createdBy = Require(request.CreatedBy, "Created by");
        var sessionId = Guid.NewGuid();

        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.TestSessions.Add(new TestSessionRecord
        {
            Id = sessionId,
            Name = name,
            TestSuiteId = Guid.Empty,
            TestSuiteRevisionId = null,
            IsManual = true,
            TestSuiteName = "Manual Session",
            TestSuiteRevisionName = null,
            TestedVersion = request.TestedVersion.Trim(),
            BuildNumber = request.BuildNumber.Trim(),
            Notes = request.Notes.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.SaveChanges();

        return sessionId;
    }

    public Guid CreateManualSection(CreateManualTestSectionRequest request)
    {
        var name = Require(request.Name, "Section name");

        using var dbContext = dbContextFactory.CreateDbContext();
        var sessionExists = dbContext.TestSessions.Any(item => item.Id == request.TestSessionId);
        if (!sessionExists)
        {
            throw new InvalidOperationException("Selected test session was not found.");
        }

        var sectionId = Guid.NewGuid();
        var sortOrder = dbContext.TestSectionResults.Count(item => item.TestSessionId == request.TestSessionId) + 1;
        dbContext.TestSectionResults.Add(new TestSectionResultRecord
        {
            Id = sectionId,
            TestSessionId = request.TestSessionId,
            TemplateSectionId = Guid.Empty,
            Name = name,
            Category = request.Category.Trim(),
            SortOrder = sortOrder
        });
        dbContext.SaveChanges();

        return sectionId;
    }

    public Guid CreateManualTestCase(CreateManualTestCaseRequest request)
    {
        var title = Require(request.Title, "Test case title");

        using var dbContext = dbContextFactory.CreateDbContext();
        var sectionExists = dbContext.TestSectionResults.Any(item => item.Id == request.TestSectionResultId);
        if (!sectionExists)
        {
            throw new InvalidOperationException("Selected section was not found.");
        }

        var testCaseId = Guid.NewGuid();
        var sortOrder = dbContext.TestCaseResults.Count(item => item.TestSectionResultId == request.TestSectionResultId) + 1;
        dbContext.TestCaseResults.Add(new TestCaseResultRecord
        {
            Id = testCaseId,
            TestSectionResultId = request.TestSectionResultId,
            TestCaseTemplateId = Guid.Empty,
            Title = title,
            ExpectedResult = request.ExpectedResult.Trim(),
            SortOrder = sortOrder,
            Status = TestResultStatus.NotTested,
            Comment = string.Empty
        });
        dbContext.SaveChanges();

        return testCaseId;
    }

    public Guid CreateManualCheck(CreateManualTestCheckRequest request)
    {
        var checkText = Require(request.CheckText, "Check text");

        using var dbContext = dbContextFactory.CreateDbContext();
        var testCaseExists = dbContext.TestCaseResults.Any(item => item.Id == request.TestCaseResultId);
        if (!testCaseExists)
        {
            throw new InvalidOperationException("Selected test case was not found.");
        }

        var checkId = Guid.NewGuid();
        var sortOrder = dbContext.TestStepResults.Count(item => item.TestCaseResultId == request.TestCaseResultId) + 1;
        dbContext.TestStepResults.Add(new TestStepResultRecord
        {
            Id = checkId,
            TestCaseResultId = request.TestCaseResultId,
            TestStepTemplateId = Guid.Empty,
            StepText = checkText,
            ExpectedResult = request.ExpectedResult.Trim(),
            SortOrder = sortOrder,
            Status = TestResultStatus.NotTested,
            Comment = string.Empty
        });
        dbContext.SaveChanges();

        return checkId;
    }

    public void UpdateTestCaseResult(UpdateTestCaseResultRequest request)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var testCase = dbContext.TestCaseResults
            .Include(item => item.Steps)
            .SingleOrDefault(item => item.Id == request.TestCaseResultId)
            ?? throw new InvalidOperationException("Selected test case result was not found.");

        testCase.Status = request.Status;
        testCase.Comment = request.Comment.Trim();

        if (request.Status is not TestResultStatus.Fail)
        {
            foreach (var step in testCase.Steps)
            {
                step.Status = request.Status;
            }
        }

        dbContext.SaveChanges();
    }

    public void UpdateTestStepResult(UpdateTestStepResultRequest request)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var step = dbContext.TestStepResults
            .Include(item => item.TestCaseResult)
            .ThenInclude(testCase => testCase!.Steps)
            .SingleOrDefault(item => item.Id == request.TestStepResultId)
            ?? throw new InvalidOperationException("Selected test step result was not found.");

        step.Status = request.Status;
        step.Comment = request.Comment.Trim();

        var testCase = step.TestCaseResult
            ?? throw new InvalidOperationException("Parent test case result was not found.");
        testCase.Status = CalculateStatusFromChecks(testCase.Steps);

        dbContext.SaveChanges();
    }

    private static TestResultStatus CalculateStatusFromChecks(IReadOnlyCollection<TestStepResultRecord> checks)
    {
        if (checks.Count == 0)
        {
            return TestResultStatus.NotTested;
        }

        if (checks.Any(check => check.Status == TestResultStatus.Fail))
        {
            return TestResultStatus.Fail;
        }

        if (checks.Any(check => check.Status == TestResultStatus.Blocked))
        {
            return TestResultStatus.Blocked;
        }

        if (checks.All(check => check.Status == TestResultStatus.NotApplicable))
        {
            return TestResultStatus.NotApplicable;
        }

        if (checks.All(check => check.Status is TestResultStatus.Pass or TestResultStatus.NotApplicable))
        {
            return TestResultStatus.Pass;
        }

        return TestResultStatus.NotTested;
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
