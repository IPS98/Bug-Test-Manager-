using BugTestManager.Infrastructure.Data.Entities;
using BugTestManager.Infrastructure.SampleData;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteDatabaseInitializer(IDbContextFactory<BugTestManagerDbContext> dbContextFactory) : IDatabaseInitializer
{
    public void Initialize()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Database.EnsureCreated();

        if (dbContext.TestSuites.Any())
        {
            return;
        }

        Seed(dbContext);
    }

    private static void Seed(BugTestManagerDbContext dbContext)
    {
        foreach (var testSuite in SampleTestSuiteCatalogSource.GetCatalog())
        {
            var testSuiteRecord = new TestSuiteRecord
            {
                Id = testSuite.Id,
                Name = testSuite.Name,
                Description = testSuite.Description,
                RevisionIsRequired = testSuite.RevisionIsRequired,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.TestSuites.Add(testSuiteRecord);

            foreach (var (revision, index) in testSuite.Revisions
                         .Where(revision => testSuite.RevisionIsRequired)
                         .Select((revision, index) => (revision, index)))
            {
                var revisionRecord = new TestSuiteRevisionRecord
                {
                    Id = revision.Id,
                    TestSuiteId = testSuite.Id,
                    Name = revision.Name,
                    EffectiveDate = revision.EffectiveDate,
                    SortOrder = index + 1
                };

                dbContext.TestSuiteRevisions.Add(revisionRecord);
            }

            foreach (var revision in testSuite.Revisions)
            {
                var revisionId = testSuite.RevisionIsRequired ? revision.Id : (Guid?)null;

                foreach (var section in revision.Sections)
                {
                    var sectionRecord = new TemplateSectionRecord
                    {
                        Id = section.Id,
                        TestSuiteId = testSuite.Id,
                        TestSuiteRevisionId = revisionId,
                        Name = section.Name,
                        Category = section.Category,
                        SortOrder = section.SortOrder
                    };

                    dbContext.TemplateSections.Add(sectionRecord);

                    foreach (var testCase in section.TestCases)
                    {
                        var testCaseRecord = new TestCaseTemplateRecord
                        {
                            Id = testCase.Id,
                            TemplateSectionId = section.Id,
                            Title = testCase.Title,
                            ExpectedResult = testCase.ExpectedResult,
                            SortOrder = testCase.SortOrder
                        };

                        dbContext.TestCaseTemplates.Add(testCaseRecord);

                        foreach (var step in testCase.Steps)
                        {
                            dbContext.TestStepTemplates.Add(new TestStepTemplateRecord
                            {
                                Id = step.Id,
                                TestCaseTemplateId = testCase.Id,
                                StepText = step.StepText,
                                ExpectedResult = step.ExpectedResult,
                                SortOrder = step.SortOrder
                            });
                        }
                    }
                }
            }
        }

        dbContext.SaveChanges();
    }
}
