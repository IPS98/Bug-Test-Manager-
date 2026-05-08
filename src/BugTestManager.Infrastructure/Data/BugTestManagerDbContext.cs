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
    }
}
