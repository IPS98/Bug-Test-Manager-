namespace BugTestManager.App.ViewModels;

public sealed record TestStepTemplateItemViewModel(
    Guid Id,
    string StepText,
    string ExpectedResult,
    int SortOrder);
