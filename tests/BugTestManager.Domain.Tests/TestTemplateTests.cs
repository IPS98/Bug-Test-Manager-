using BugTestManager.Domain.Entities;

namespace BugTestManager.Domain.Tests;

public sealed class TestTemplateTests
{
    [Fact]
    public void AddRevision_StoresTemplateRevision()
    {
        var template = CreateTemplate();

        var revision = template.AddRevision("Revision A");

        Assert.Equal(template.Id, revision.TestTemplateId);
        Assert.Equal("Revision A", revision.RevisionName);
        Assert.Single(template.Revisions);
    }

    [Fact]
    public void AddRevision_DoesNotAllowDuplicateRevisionNames()
    {
        var template = CreateTemplate();
        template.AddRevision("Revision A");

        Assert.Throws<InvalidOperationException>(() => template.AddRevision("revision a"));
    }

    [Fact]
    public void TemplateHierarchy_CanStoreSectionCaseAndStep()
    {
        var template = CreateTemplate();
        var revision = template.AddRevision("Revision A");
        var section = revision.AddSection("Normal", "Functional Section", sortOrder: 1);
        var testCase = section.AddTestCase("Input voltage test", expectedResult: "Voltage is accepted", sortOrder: 1);

        var step = testCase.AddStep("Set input voltage to nominal value", "Application shows normal status", 1);

        Assert.Equal(revision.Id, section.TemplateRevisionId);
        Assert.Equal(section.Id, testCase.TemplateSectionId);
        Assert.Equal(testCase.Id, step.TestCaseTemplateId);
        Assert.Single(revision.Sections);
        Assert.Single(section.TestCases);
        Assert.Single(testCase.Steps);
    }

    [Fact]
    public void TemplateSection_DoesNotAllowNegativeSortOrder()
    {
        var template = CreateTemplate();
        var revision = template.AddRevision("Revision A");

        Assert.Throws<ArgumentOutOfRangeException>(() => revision.AddSection("Normal", sortOrder: -1));
    }

    private static TestTemplate CreateTemplate()
    {
        var product = new Product("Power Supply Manager");
        var testSuite = product.AddTestSuite("Electrical checks");
        var revision = testSuite.AddRevision("Revision A");

        return new TestTemplate(testSuite.Id, revision.Id, "Electrical Test Template");
    }
}
