namespace BugTestManager.App.ViewModels;

public sealed record TestStepTemplateItemViewModel(
    string StepText,
    string ExpectedResult,
    int SortOrder);
