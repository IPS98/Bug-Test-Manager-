using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class TestCaseTemplate
{
    private readonly List<TestStepTemplate> steps = [];

    public TestCaseTemplate(Guid templateSectionId, string title, string? description = null, string? expectedResult = null, int sortOrder = 0)
    {
        Id = Guid.NewGuid();
        TemplateSectionId = Guard.Required(templateSectionId, nameof(templateSectionId), "Template section id");
        Title = Guard.Required(title, nameof(title), "Test case title");
        Description = description?.Trim() ?? string.Empty;
        ExpectedResult = expectedResult?.Trim() ?? string.Empty;
        SortOrder = Guard.NotNegative(sortOrder, nameof(sortOrder), "Sort order");
    }

    public Guid Id { get; }

    public Guid TemplateSectionId { get; }

    public string Title { get; }

    public string Description { get; }

    public string ExpectedResult { get; }

    public int SortOrder { get; }

    public IReadOnlyCollection<TestStepTemplate> Steps => steps.AsReadOnly();

    public TestStepTemplate AddStep(string stepText, string? expectedResult = null, int sortOrder = 0)
    {
        var step = new TestStepTemplate(Id, stepText, expectedResult, sortOrder);
        steps.Add(step);

        return step;
    }
}
