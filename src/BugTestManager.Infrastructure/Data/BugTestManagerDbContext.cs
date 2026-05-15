using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class BugTestManagerDbContext(DbContextOptions<BugTestManagerDbContext> options) : DbContext(options)
{
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

    public DbSet<TestSuiteRecord> TestSuites => Set<TestSuiteRecord>();

    public DbSet<TestSuiteRevisionRecord> TestSuiteRevisions => Set<TestSuiteRevisionRecord>();

    public DbSet<TemplateSectionRecord> TemplateSections => Set<TemplateSectionRecord>();

    public DbSet<TestCaseTemplateRecord> TestCaseTemplates => Set<TestCaseTemplateRecord>();

    public DbSet<TestStepTemplateRecord> TestStepTemplates => Set<TestStepTemplateRecord>();

    public DbSet<CustomFieldDefinitionRecord> CustomFieldDefinitions => Set<CustomFieldDefinitionRecord>();

    public DbSet<CustomFieldDefinitionScopeRecord> CustomFieldDefinitionScopes => Set<CustomFieldDefinitionScopeRecord>();

    public DbSet<CustomFieldValueRecord> CustomFieldValues => Set<CustomFieldValueRecord>();

    public DbSet<TestSessionRecord> TestSessions => Set<TestSessionRecord>();

    public DbSet<TestSectionResultRecord> TestSectionResults => Set<TestSectionResultRecord>();

    public DbSet<TestCaseResultRecord> TestCaseResults => Set<TestCaseResultRecord>();

    public DbSet<TestStepResultRecord> TestStepResults => Set<TestStepResultRecord>();

    public DbSet<AttachmentRecord> Attachments => Set<AttachmentRecord>();

    public DbSet<BugReportRecord> BugReports => Set<BugReportRecord>();

    public DbSet<DiscussionCommentRecord> DiscussionComments => Set<DiscussionCommentRecord>();

    public DbSet<DiscussionReadStateRecord> DiscussionReadStates => Set<DiscussionReadStateRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectRecord>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(project => project.Name).IsUnique();
        });

        modelBuilder.Entity<TestSuiteRecord>(entity =>
        {
            entity.ToTable("TestSuites");
            entity.HasKey(testSuite => testSuite.Id);
            entity.Property(testSuite => testSuite.ProjectId).IsRequired();
            entity.Property(testSuite => testSuite.Name).HasMaxLength(200).IsRequired();
            entity.Property(testSuite => testSuite.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(testSuite => new { testSuite.ProjectId, testSuite.Name }).IsUnique();
        });

        modelBuilder.Entity<TestSuiteRevisionRecord>(entity =>
        {
            entity.ToTable("TestSuiteRevisions");
            entity.HasKey(revision => revision.Id);
            entity.Property(revision => revision.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(revision => new { revision.TestSuiteId, revision.Name }).IsUnique();
            entity.HasOne(revision => revision.TestSuite)
                .WithMany(testSuite => testSuite.Revisions)
                .HasForeignKey(revision => revision.TestSuiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateSectionRecord>(entity =>
        {
            entity.ToTable("TemplateSections");
            entity.HasKey(section => section.Id);
            entity.Property(section => section.Name).HasMaxLength(200).IsRequired();
            entity.Property(section => section.Category).HasMaxLength(200).IsRequired();
            entity.HasOne(section => section.TestSuite)
                .WithMany(testSuite => testSuite.Sections)
                .HasForeignKey(section => section.TestSuiteId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(section => section.TestSuiteRevision)
                .WithMany(revision => revision.Sections)
                .HasForeignKey(section => section.TestSuiteRevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestCaseTemplateRecord>(entity =>
        {
            entity.ToTable("TestCaseTemplates");
            entity.HasKey(testCase => testCase.Id);
            entity.Property(testCase => testCase.Title).HasMaxLength(300).IsRequired();
            entity.Property(testCase => testCase.ExpectedResult).HasMaxLength(4000).IsRequired();
            entity.HasOne(testCase => testCase.TemplateSection)
                .WithMany(section => section.TestCases)
                .HasForeignKey(testCase => testCase.TemplateSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestStepTemplateRecord>(entity =>
        {
            entity.ToTable("TestStepTemplates");
            entity.HasKey(step => step.Id);
            entity.Property(step => step.StepText).HasMaxLength(4000).IsRequired();
            entity.Property(step => step.ExpectedResult).HasMaxLength(4000).IsRequired();
            entity.HasOne(step => step.TestCaseTemplate)
                .WithMany(testCase => testCase.Steps)
                .HasForeignKey(step => step.TestCaseTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomFieldDefinitionRecord>(entity =>
        {
            entity.ToTable("CustomFieldDefinitions");
            entity.HasKey(field => field.Id);
            entity.Property(field => field.ProjectId).IsRequired();
            entity.Property(field => field.Name).HasMaxLength(200).IsRequired();
            entity.Property(field => field.ScopeDisplayName).HasMaxLength(500).IsRequired();
            entity.Property(field => field.OptionsJson).HasMaxLength(4000).IsRequired();
            entity.HasIndex(field => new { field.ProjectId, field.TargetEntityType, field.ScopeType, field.ScopeEntityId, field.Name });
        });

        modelBuilder.Entity<CustomFieldDefinitionScopeRecord>(entity =>
        {
            entity.ToTable("CustomFieldDefinitionScopes");
            entity.HasKey(scope => scope.Id);
            entity.Property(scope => scope.ScopeDisplayName).HasMaxLength(500).IsRequired();
            entity.HasIndex(scope => new { scope.FieldDefinitionId, scope.ScopeType, scope.ScopeEntityId }).IsUnique();
            entity.HasOne(scope => scope.FieldDefinition)
                .WithMany(field => field.Scopes)
                .HasForeignKey(scope => scope.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomFieldValueRecord>(entity =>
        {
            entity.ToTable("CustomFieldValues");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.ValueJson).HasMaxLength(4000).IsRequired();
            entity.Property(value => value.UpdatedBy).HasMaxLength(200).IsRequired();
            entity.HasIndex(value => new { value.EntityType, value.EntityId });
            entity.HasIndex(value => new { value.FieldDefinitionId, value.EntityType, value.EntityId }).IsUnique();
            entity.HasOne(value => value.FieldDefinition)
                .WithMany()
                .HasForeignKey(value => value.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestSessionRecord>(entity =>
        {
            entity.ToTable("TestSessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.ProjectId).IsRequired();
            entity.Property(session => session.Name).HasMaxLength(300).IsRequired();
            entity.Property(session => session.IsManual).IsRequired();
            entity.Property(session => session.TestSuiteName).HasMaxLength(200).IsRequired();
            entity.Property(session => session.TestSuiteRevisionName).HasMaxLength(200);
            entity.Property(session => session.TestedVersion).HasMaxLength(100).IsRequired();
            entity.Property(session => session.BuildNumber).HasMaxLength(100).IsRequired();
            entity.Property(session => session.Notes).HasMaxLength(4000).IsRequired();
            entity.Property(session => session.CreatedBy).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<TestSectionResultRecord>(entity =>
        {
            entity.ToTable("TestSectionResults");
            entity.HasKey(section => section.Id);
            entity.Property(section => section.Name).HasMaxLength(200).IsRequired();
            entity.Property(section => section.Category).HasMaxLength(200).IsRequired();
            entity.HasOne(section => section.TestSession)
                .WithMany(session => session.Sections)
                .HasForeignKey(section => section.TestSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestCaseResultRecord>(entity =>
        {
            entity.ToTable("TestCaseResults");
            entity.HasKey(testCase => testCase.Id);
            entity.Property(testCase => testCase.Title).HasMaxLength(300).IsRequired();
            entity.Property(testCase => testCase.ExpectedResult).HasMaxLength(4000).IsRequired();
            entity.Property(testCase => testCase.Comment).HasMaxLength(4000).IsRequired();
            entity.HasOne(testCase => testCase.TestSectionResult)
                .WithMany(section => section.TestCases)
                .HasForeignKey(testCase => testCase.TestSectionResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestStepResultRecord>(entity =>
        {
            entity.ToTable("TestStepResults");
            entity.HasKey(step => step.Id);
            entity.Property(step => step.StepText).HasMaxLength(4000).IsRequired();
            entity.Property(step => step.ExpectedResult).HasMaxLength(4000).IsRequired();
            entity.Property(step => step.Comment).HasMaxLength(4000).IsRequired();
            entity.HasOne(step => step.TestCaseResult)
                .WithMany(testCase => testCase.Steps)
                .HasForeignKey(step => step.TestCaseResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttachmentRecord>(entity =>
        {
            entity.ToTable("Attachments");
            entity.HasKey(attachment => attachment.Id);
            entity.Property(attachment => attachment.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(attachment => attachment.StoredFileName).HasMaxLength(260).IsRequired();
            entity.Property(attachment => attachment.RelativePath).HasMaxLength(1000).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(attachment => attachment.UploadedBy).HasMaxLength(200).IsRequired();
            entity.HasIndex(attachment => new { attachment.EntityType, attachment.EntityId, attachment.UploadedAt });
        });

        modelBuilder.Entity<BugReportRecord>(entity =>
        {
            entity.ToTable("BugReports");
            entity.HasKey(bug => bug.Id);
            entity.Property(bug => bug.ProjectId).IsRequired();
            entity.Property(bug => bug.Title).HasMaxLength(300).IsRequired();
            entity.Property(bug => bug.Description).HasMaxLength(4000).IsRequired();
            entity.Property(bug => bug.Severity).HasMaxLength(100).IsRequired();
            entity.Property(bug => bug.Priority).HasMaxLength(100).IsRequired();
            entity.Property(bug => bug.FoundInVersion).HasMaxLength(100).IsRequired();
            entity.Property(bug => bug.BuildNumber).HasMaxLength(100).IsRequired();
            entity.Property(bug => bug.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(bug => bug.UpdatedBy).HasMaxLength(200).IsRequired();
            entity.Property(bug => bug.LinkedEntityDisplayName).HasMaxLength(500).IsRequired();
            entity.HasIndex(bug => new { bug.ProjectId, bug.Status, bug.UpdatedAt });
            entity.HasIndex(bug => new { bug.LinkedEntityType, bug.LinkedEntityId });
        });

        modelBuilder.Entity<DiscussionCommentRecord>(entity =>
        {
            entity.ToTable("DiscussionComments");
            entity.HasKey(comment => comment.Id);
            entity.Property(comment => comment.Message).HasMaxLength(4000).IsRequired();
            entity.Property(comment => comment.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(comment => comment.UpdatedBy).HasMaxLength(200).IsRequired();
            entity.HasIndex(comment => new { comment.EntityType, comment.EntityId, comment.CreatedAt });
        });

        modelBuilder.Entity<DiscussionReadStateRecord>(entity =>
        {
            entity.ToTable("DiscussionReadStates");
            entity.HasKey(readState => readState.Id);
            entity.Property(readState => readState.UserName).HasMaxLength(200).IsRequired();
            entity.HasIndex(readState => new { readState.EntityType, readState.EntityId, readState.UserName }).IsUnique();
        });
    }
}
