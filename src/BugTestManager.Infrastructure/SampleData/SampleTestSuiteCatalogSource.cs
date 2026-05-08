using BugTestManager.Application.ReadModels;

namespace BugTestManager.Infrastructure.SampleData;

public static class SampleTestSuiteCatalogSource
{
    public static IReadOnlyList<TestSuiteCatalogItem> GetCatalog()
    {
        return
        [
            new TestSuiteCatalogItem(
                Guid.Parse("2b061087-3671-4e2a-a6d4-d1d8f34f1b01"),
                "Power Module Acceptance",
                "Reusable checks for power module releases and firmware combinations.",
                true,
                [
                    new TestSuiteRevisionCatalogItem(
                        Guid.Parse("20cf5606-f024-4a60-adc4-14ac0d22a111"),
                        "Revision A",
                        new DateOnly(2026, 5, 8),
                        [
                            CreateSection(
                                Guid.Parse("8b7fa79d-6eb5-4d6d-87aa-d6c3cf046201"),
                                "Normal",
                                "Operating mode",
                                1,
                                [
                                    CreateCase(
                                        Guid.Parse("f98d7f87-34d9-4f19-9f21-669460f51301"),
                                        "Startup sequence",
                                        "The application shows ready state without errors.",
                                        1,
                                        [
                                            CreateStep(Guid.Parse("17bcde22-ad3e-4f36-9706-04cc63420101"), "Select the tested model.", "The model is displayed in the session header.", 1),
                                            CreateStep(Guid.Parse("9ccf063e-bad2-4b96-a9ef-86df57140102"), "Connect the unit and refresh status.", "Connection status changes to ready.", 2),
                                            CreateStep(Guid.Parse("731e5648-3c6c-4ae0-b366-e3aaf0e10103"), "Start the standard startup check.", "All startup indicators are green.", 3)
                                        ]),
                                    CreateCase(
                                        Guid.Parse("8601de7b-e9a8-42d5-af05-73f9df551302"),
                                        "Telemetry table refresh",
                                        "Values update without frozen rows or stale timestamps.",
                                        2,
                                        [
                                            CreateStep(Guid.Parse("0367da6f-2e7d-4f06-80e0-ce9d0d450201"), "Open telemetry table.", "Table is visible and contains rows.", 1),
                                            CreateStep(Guid.Parse("9dc9696f-0d56-41e3-b7ad-c50c74010202"), "Wait for the next refresh interval.", "Timestamp and values are updated.", 2)
                                        ])
                                ]),
                            CreateSection(
                                Guid.Parse("26b47f9b-3e33-4c0a-94bf-467f91ca6202"),
                                "Abnormal",
                                "Fault mode",
                                2,
                                [
                                    CreateCase(
                                        Guid.Parse("bdcaaeb6-b3ec-4260-a2bb-d4fd10392301"),
                                        "Communication loss",
                                        "The application shows a clear warning and keeps previous evidence.",
                                        1,
                                        [
                                            CreateStep(Guid.Parse("ca8a9d24-301a-4bf1-a3a2-e77240300301"), "Disconnect communication cable.", "Connection status changes to lost.", 1),
                                            CreateStep(Guid.Parse("690332d8-bc30-4b1e-9f39-3e807c9c0302"), "Reconnect communication cable.", "Status returns to ready after refresh.", 2)
                                        ])
                                ])
                        ]),
                    new TestSuiteRevisionCatalogItem(
                        Guid.Parse("018a2c99-1376-4ee3-9d60-47df51e6a112"),
                        "Revision B",
                        null,
                        [
                            CreateSection(
                                Guid.Parse("2e4d9ce7-33d8-4b0a-afc7-20cb7d806203"),
                                "Normal",
                                "Operating mode",
                                1,
                                [
                                    CreateCase(
                                        Guid.Parse("6c25cf2f-b8c0-402c-b34f-2d52bfc63301"),
                                        "Firmware version display",
                                        "Firmware version is visible and report-ready.",
                                        1,
                                        [
                                            CreateStep(Guid.Parse("c0ce6e84-c432-43f7-aa51-cc87db310401"), "Open device details.", "Firmware version field is visible.", 1),
                                            CreateStep(Guid.Parse("b52f92c0-9536-4a2c-8f8a-ee5eec0f0402"), "Compare value with release notes.", "Displayed firmware matches the expected version.", 2)
                                        ])
                                ])
                        ])
                ]),
            new TestSuiteCatalogItem(
                Guid.Parse("eed47c43-5d07-4cc5-86f1-9240a09a1b02"),
                "Application UI Regression",
                "Reusable checks for windows, tabs, tables, graphs, and controls.",
                false,
                [
                    new TestSuiteRevisionCatalogItem(
                        Guid.Empty,
                        "No revision",
                        new DateOnly(2026, 5, 8),
                        [
                            CreateSection(
                                Guid.Parse("c1671cf2-a5c8-43da-a64c-e6b5c90d6401"),
                                "Main Window",
                                "UI",
                                1,
                                [
                                    CreateCase(
                                        Guid.Parse("96f2bc9d-c79d-4ca1-ae56-9293603f6501"),
                                        "Navigation panel",
                                        "All main pages are visible and selectable.",
                                        1,
                                        [
                                            CreateStep(Guid.Parse("8e3d3672-c5da-4a46-9a8a-e3c51b5d0601"), "Open the application.", "Navigation panel is visible.", 1),
                                            CreateStep(Guid.Parse("88e793c0-a376-4833-888a-c953f7e60602"), "Select each page.", "Selected page content is shown.", 2)
                                        ])
                                ]),
                            CreateSection(
                                Guid.Parse("aa855f0d-f27c-41a4-a2b4-752d362e6402"),
                                "Reports",
                                "Export",
                                2,
                                [
                                    CreateCase(
                                        Guid.Parse("40fe9f5d-29ed-4e22-b434-00dd96436502"),
                                        "Report preview",
                                        "Preview contains session metadata, statuses, and attachments.",
                                        1,
                                        [
                                            CreateStep(Guid.Parse("adf61f6b-45f7-41d2-9e64-7cf86d410701"), "Open report preview.", "Report preview is displayed.", 1),
                                            CreateStep(Guid.Parse("9ce71475-3da3-46fd-b13a-65e8c6fb0702"), "Check summary section.", "Counts and statuses are readable.", 2)
                                        ])
                                ])
                        ])
                ])
        ];
    }

    private static TemplateSectionCatalogItem CreateSection(
        Guid id,
        string name,
        string category,
        int sortOrder,
        IReadOnlyList<TestCaseTemplateCatalogItem> testCases)
    {
        return new TemplateSectionCatalogItem(id, name, category, sortOrder, testCases);
    }

    private static TestCaseTemplateCatalogItem CreateCase(
        Guid id,
        string title,
        string expectedResult,
        int sortOrder,
        IReadOnlyList<TestStepTemplateCatalogItem> steps)
    {
        return new TestCaseTemplateCatalogItem(id, title, expectedResult, sortOrder, steps);
    }

    private static TestStepTemplateCatalogItem CreateStep(Guid id, string stepText, string expectedResult, int sortOrder)
    {
        return new TestStepTemplateCatalogItem(id, stepText, expectedResult, sortOrder);
    }
}
