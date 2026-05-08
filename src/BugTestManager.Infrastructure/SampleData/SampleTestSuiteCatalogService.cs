using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;

namespace BugTestManager.Infrastructure.SampleData;

public sealed class SampleTestSuiteCatalogService : ITestSuiteCatalogService
{
    public IReadOnlyList<TestSuiteCatalogItem> GetCatalog()
    {
        return
        [
            new TestSuiteCatalogItem(
                Guid.NewGuid(),
                "Power Module Acceptance",
                "Reusable checks for power module releases and firmware combinations.",
                [
                    new TestSuiteRevisionCatalogItem(
                        Guid.NewGuid(),
                        "Revision A",
                        new DateOnly(2026, 5, 8),
                        [
                            CreateSection(
                                "Normal",
                                "Operating mode",
                                1,
                                [
                                    CreateCase(
                                        "Startup sequence",
                                        "The application shows ready state without errors.",
                                        1,
                                        [
                                            CreateStep("Select the tested model.", "The model is displayed in the session header.", 1),
                                            CreateStep("Connect the unit and refresh status.", "Connection status changes to ready.", 2),
                                            CreateStep("Start the standard startup check.", "All startup indicators are green.", 3)
                                        ]),
                                    CreateCase(
                                        "Telemetry table refresh",
                                        "Values update without frozen rows or stale timestamps.",
                                        2,
                                        [
                                            CreateStep("Open telemetry table.", "Table is visible and contains rows.", 1),
                                            CreateStep("Wait for the next refresh interval.", "Timestamp and values are updated.", 2)
                                        ])
                                ]),
                            CreateSection(
                                "Abnormal",
                                "Fault mode",
                                2,
                                [
                                    CreateCase(
                                        "Communication loss",
                                        "The application shows a clear warning and keeps previous evidence.",
                                        1,
                                        [
                                            CreateStep("Disconnect communication cable.", "Connection status changes to lost.", 1),
                                            CreateStep("Reconnect communication cable.", "Status returns to ready after refresh.", 2)
                                        ])
                                ])
                        ]),
                    new TestSuiteRevisionCatalogItem(
                        Guid.NewGuid(),
                        "Revision B",
                        null,
                        [
                            CreateSection(
                                "Normal",
                                "Operating mode",
                                1,
                                [
                                    CreateCase(
                                        "Firmware version display",
                                        "Firmware version is visible and report-ready.",
                                        1,
                                        [
                                            CreateStep("Open device details.", "Firmware version field is visible.", 1),
                                            CreateStep("Compare value with release notes.", "Displayed firmware matches the expected version.", 2)
                                        ])
                                ])
                        ])
                ]),
            new TestSuiteCatalogItem(
                Guid.NewGuid(),
                "Application UI Regression",
                "Reusable checks for windows, tabs, tables, graphs, and controls.",
                [
                    new TestSuiteRevisionCatalogItem(
                        Guid.NewGuid(),
                        "Revision A",
                        new DateOnly(2026, 5, 8),
                        [
                            CreateSection(
                                "Main Window",
                                "UI",
                                1,
                                [
                                    CreateCase(
                                        "Navigation panel",
                                        "All main pages are visible and selectable.",
                                        1,
                                        [
                                            CreateStep("Open the application.", "Navigation panel is visible.", 1),
                                            CreateStep("Select each page.", "Selected page content is shown.", 2)
                                        ])
                                ]),
                            CreateSection(
                                "Reports",
                                "Export",
                                2,
                                [
                                    CreateCase(
                                        "Report preview",
                                        "Preview contains session metadata, statuses, and attachments.",
                                        1,
                                        [
                                            CreateStep("Open report preview.", "Report preview is displayed.", 1),
                                            CreateStep("Check summary section.", "Counts and statuses are readable.", 2)
                                        ])
                                ])
                        ])
                ])
        ];
    }

    private static TemplateSectionCatalogItem CreateSection(
        string name,
        string category,
        int sortOrder,
        IReadOnlyList<TestCaseTemplateCatalogItem> testCases)
    {
        return new TemplateSectionCatalogItem(Guid.NewGuid(), name, category, sortOrder, testCases);
    }

    private static TestCaseTemplateCatalogItem CreateCase(
        string title,
        string expectedResult,
        int sortOrder,
        IReadOnlyList<TestStepTemplateCatalogItem> steps)
    {
        return new TestCaseTemplateCatalogItem(Guid.NewGuid(), title, expectedResult, sortOrder, steps);
    }

    private static TestStepTemplateCatalogItem CreateStep(string stepText, string expectedResult, int sortOrder)
    {
        return new TestStepTemplateCatalogItem(Guid.NewGuid(), stepText, expectedResult, sortOrder);
    }
}
