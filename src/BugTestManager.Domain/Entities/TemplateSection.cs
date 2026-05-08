using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TemplateSection
{
    private readonly List<TestCaseTemplate> testCases = [];

    public TemplateSection(Guid templateRevisionId, string name, string? category = null, Guid? parentSectionId = null, int sortOrder = 0)
    {
        Id = Guid.NewGuid();
        TemplateRevisionId = Guard.Required(templateRevisionId, nameof(templateRevisionId), "Template revision id");
        ParentSectionId = parentSectionId;
        Name = Guard.Required(name, nameof(name), "Section name");
        Category = category?.Trim() ?? string.Empty;
        SortOrder = Guard.NotNegative(sortOrder, nameof(sortOrder), "Sort order");
    }

    public Guid Id { get; }

    public Guid TemplateRevisionId { get; }

    public Guid? ParentSectionId { get; }

    public string Name { get; }

    public string Category { get; }

    public int SortOrder { get; }

    public IReadOnlyCollection<TestCaseTemplate> TestCases => testCases.AsReadOnly();

    public TestCaseTemplate AddTestCase(string title, string? description = null, string? expectedResult = null, int sortOrder = 0)
    {
        var testCase = new TestCaseTemplate(Id, title, description, expectedResult, sortOrder);
        testCases.Add(testCase);

        return testCase;
    }
}
