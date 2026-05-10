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
        EnsureCustomFieldDefinitionTable(dbContext);
        EnsureTestSessionTables(dbContext);
        EnsureAttachmentTable(dbContext);

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

    private static void EnsureCustomFieldDefinitionTable(BugTestManagerDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "CustomFieldDefinitions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_CustomFieldDefinitions" PRIMARY KEY,
                "TargetEntityType" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                "FieldType" INTEGER NOT NULL,
                "IsRequired" INTEGER NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                "ScopeType" INTEGER NOT NULL DEFAULT 0,
                "ScopeEntityId" TEXT NULL,
                "ScopeDisplayName" TEXT NOT NULL DEFAULT 'All matching items',
                "IsActive" INTEGER NOT NULL,
                "OptionsJson" TEXT NOT NULL
            );
            """);

        EnsureColumn(dbContext, "CustomFieldDefinitions", "ScopeType", "\"ScopeType\" INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(dbContext, "CustomFieldDefinitions", "ScopeEntityId", "\"ScopeEntityId\" TEXT NULL");
        EnsureColumn(
            dbContext,
            "CustomFieldDefinitions",
            "ScopeDisplayName",
            "\"ScopeDisplayName\" TEXT NOT NULL DEFAULT 'All matching items'");

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_CustomFieldDefinitions_TargetEntityType_Name"
            ON "CustomFieldDefinitions" ("TargetEntityType", "Name");
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_CustomFieldDefinitions_TargetEntityType_ScopeType_ScopeEntityId_Name"
            ON "CustomFieldDefinitions" ("TargetEntityType", "ScopeType", "ScopeEntityId", "Name");
            """);
    }

    private static void EnsureTestSessionTables(BugTestManagerDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "TestSessions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TestSessions" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "TestSuiteId" TEXT NOT NULL,
                "TestSuiteRevisionId" TEXT NULL,
                "TestSuiteName" TEXT NOT NULL,
                "TestSuiteRevisionName" TEXT NULL,
                "TestedVersion" TEXT NOT NULL,
                "BuildNumber" TEXT NOT NULL,
                "Notes" TEXT NOT NULL,
                "CreatedBy" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "TestSectionResults" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TestSectionResults" PRIMARY KEY,
                "TestSessionId" TEXT NOT NULL,
                "TemplateSectionId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "Category" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                CONSTRAINT "FK_TestSectionResults_TestSessions_TestSessionId"
                    FOREIGN KEY ("TestSessionId") REFERENCES "TestSessions" ("Id") ON DELETE CASCADE
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "TestCaseResults" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TestCaseResults" PRIMARY KEY,
                "TestSectionResultId" TEXT NOT NULL,
                "TestCaseTemplateId" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "ExpectedResult" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "Comment" TEXT NOT NULL,
                CONSTRAINT "FK_TestCaseResults_TestSectionResults_TestSectionResultId"
                    FOREIGN KEY ("TestSectionResultId") REFERENCES "TestSectionResults" ("Id") ON DELETE CASCADE
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "TestStepResults" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TestStepResults" PRIMARY KEY,
                "TestCaseResultId" TEXT NOT NULL,
                "TestStepTemplateId" TEXT NOT NULL,
                "StepText" TEXT NOT NULL,
                "ExpectedResult" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "Comment" TEXT NOT NULL,
                CONSTRAINT "FK_TestStepResults_TestCaseResults_TestCaseResultId"
                    FOREIGN KEY ("TestCaseResultId") REFERENCES "TestCaseResults" ("Id") ON DELETE CASCADE
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_TestSectionResults_TestSessionId"
            ON "TestSectionResults" ("TestSessionId");
            """);
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_TestCaseResults_TestSectionResultId"
            ON "TestCaseResults" ("TestSectionResultId");
            """);
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_TestStepResults_TestCaseResultId"
            ON "TestStepResults" ("TestCaseResultId");
            """);
    }

    private static void EnsureAttachmentTable(BugTestManagerDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "Attachments" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Attachments" PRIMARY KEY,
                "EntityType" INTEGER NOT NULL,
                "EntityId" TEXT NOT NULL,
                "OriginalFileName" TEXT NOT NULL,
                "StoredFileName" TEXT NOT NULL,
                "RelativePath" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                "UploadedBy" TEXT NOT NULL,
                "UploadedAt" TEXT NOT NULL
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE INDEX IF NOT EXISTS "IX_Attachments_EntityType_EntityId_UploadedAt"
            ON "Attachments" ("EntityType", "EntityId", "UploadedAt");
            """);
    }

    private static void EnsureColumn(
        BugTestManagerDbContext dbContext,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        var connection = dbContext.Database.GetDbConnection();
        dbContext.Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        var columnExists = false;
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }
        }

        if (columnExists)
        {
            return;
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN {columnDefinition};";
        alterCommand.ExecuteNonQuery();
    }
}
