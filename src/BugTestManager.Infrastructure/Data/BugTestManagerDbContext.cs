using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class BugTestManagerDbContext(DbContextOptions<BugTestManagerDbContext> options) : DbContext(options)
{
    public DbSet<TestSuiteRecord> TestSuites => Set<TestSuiteRecord>();

    public DbSet<TestSuiteRevisionRecord> TestSuiteRevisions => Set<TestSuiteRevisionRecord>();

    public DbSet<TemplateSectionRecord> TemplateSections => Set<TemplateSectionRecord>();

    public DbSet<TestCaseTemplateRecord> TestCaseTemplates => Set<TestCaseTemplateRecord>();

    public DbSet<TestStepTemplateRecord> TestStepTemplates => Set<TestStepTemplateRecord>();

    public DbSet<CustomFieldDefinitionRecord> CustomFieldDefinitions => Set<CustomFieldDefinitionRecord>();

    public DbSet<TestSessionRecord> TestSessions => Set<TestSessionRecord>();

    public DbSet<TestSectionResultRecord> TestSectionResults => Set<TestSectionResultRecord>();

    public DbSet<TestCaseResultRecord> TestCaseResults => Set<TestCaseResultRecord>();

    public DbSet<TestStepResultRecord> TestStepResults => Set<TestStepResultRecord>();

    public DbSet<AttachmentRecord> Attachments => Set<AttachmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestSuiteRecord>(entity =>
        {
            entity.ToTable("TestSuites");
            entity.HasKey(testSuite => testSuite.Id);
            entity.Property(testSuite => testSuite.Name).HasMaxLength(200).IsRequired();
            entity.Property(testSuite => testSuite.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(testSuite => testSuite.Name).IsUnique();
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
            entity.Property(field => field.Name).HasMaxLength(200).IsRequired();
            entity.Property(field => field.ScopeDisplayName).HasMaxLength(500).IsRequired();
            entity.Property(field => field.OptionsJson).HasMaxLength(4000).IsRequired();
            entity.HasIndex(field => new { field.TargetEntityType, field.ScopeType, field.ScopeEntityId, field.Name });
        });

        modelBuilder.Entity<TestSessionRecord>(entity =>
        {
            entity.ToTable("TestSessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Name).HasMaxLength(300).IsRequired();
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
    }
}
