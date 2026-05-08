namespace BugTestManager.Application.ReadModels;

public sealed record TestSuiteCatalogItem(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<TestSuiteRevisionCatalogItem> Revisions);

public sealed record TestSuiteRevisionCatalogItem(
    Guid Id,
    string Name,
    DateOnly? EffectiveDate,
    IReadOnlyList<TemplateSectionCatalogItem> Sections);

public sealed record TemplateSectionCatalogItem(
    Guid Id,
    string Name,
    string Category,
    int SortOrder,
    IReadOnlyList<TestCaseTemplateCatalogItem> TestCases);

public sealed record TestCaseTemplateCatalogItem(
    Guid Id,
    string Title,
    string ExpectedResult,
    int SortOrder,
    IReadOnlyList<TestStepTemplateCatalogItem> Steps);

public sealed record TestStepTemplateCatalogItem(
    Guid Id,
    string StepText,
    string ExpectedResult,
    int SortOrder);
