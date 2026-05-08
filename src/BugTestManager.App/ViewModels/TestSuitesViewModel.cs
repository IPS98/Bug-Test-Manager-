using System.Collections.ObjectModel;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BugTestManager.App.ViewModels;

public sealed partial class TestSuitesViewModel : ObservableObject
{
    public TestSuitesViewModel(ITestSuiteCatalogService catalogService)
    {
        TestSuites = new ObservableCollection<TestSuiteItemViewModel>(
            catalogService.GetCatalog().Select(MapTestSuite));

        SelectedTestSuite = TestSuites.FirstOrDefault();
    }

    public ObservableCollection<TestSuiteItemViewModel> TestSuites { get; }

    [ObservableProperty]
    private TestSuiteItemViewModel? selectedTestSuite;

    [ObservableProperty]
    private TestSuiteRevisionItemViewModel? selectedRevision;

    [ObservableProperty]
    private TemplateSectionItemViewModel? selectedSection;

    [ObservableProperty]
    private TestCaseTemplateItemViewModel? selectedTestCase;

    partial void OnSelectedTestSuiteChanged(TestSuiteItemViewModel? value)
    {
        SelectedRevision = value?.Revisions.FirstOrDefault();
    }

    partial void OnSelectedRevisionChanged(TestSuiteRevisionItemViewModel? value)
    {
        SelectedSection = value?.Sections.FirstOrDefault();
    }

    partial void OnSelectedSectionChanged(TemplateSectionItemViewModel? value)
    {
        SelectedTestCase = value?.TestCases.FirstOrDefault();
    }

    private static TestSuiteItemViewModel MapTestSuite(TestSuiteCatalogItem testSuite)
    {
        return new TestSuiteItemViewModel(
            testSuite.Name,
            testSuite.Description,
            testSuite.Revisions.Select(MapRevision));
    }

    private static TestSuiteRevisionItemViewModel MapRevision(TestSuiteRevisionCatalogItem revision)
    {
        return new TestSuiteRevisionItemViewModel(
            revision.Name,
            revision.EffectiveDate,
            revision.Sections
                .OrderBy(section => section.SortOrder)
                .Select(MapSection));
    }

    private static TemplateSectionItemViewModel MapSection(TemplateSectionCatalogItem section)
    {
        return new TemplateSectionItemViewModel(
            section.Name,
            section.Category,
            section.SortOrder,
            section.TestCases
                .OrderBy(testCase => testCase.SortOrder)
                .Select(MapTestCase));
    }

    private static TestCaseTemplateItemViewModel MapTestCase(TestCaseTemplateCatalogItem testCase)
    {
        return new TestCaseTemplateItemViewModel(
            testCase.Title,
            testCase.ExpectedResult,
            testCase.SortOrder,
            testCase.Steps
                .OrderBy(step => step.SortOrder)
                .Select(MapStep));
    }

    private static TestStepTemplateItemViewModel MapStep(TestStepTemplateCatalogItem step)
    {
        return new TestStepTemplateItemViewModel(
            step.StepText,
            step.ExpectedResult,
            step.SortOrder);
    }
}
