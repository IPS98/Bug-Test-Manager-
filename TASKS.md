# Bug & Test Manager - Tasks

Status: implementation in progress after approved architecture and skeleton setup.

## Current Progress

- Repository documentation created.
- Solution file created.
- WPF app project created.
- Domain, Application, and Infrastructure projects created.
- MahApps.Metro connected to the WPF app.
- CommunityToolkit.Mvvm connected for ViewModels.
- Dependency injection connected for application startup.
- Initial Domain test project created.
- Initial build passed.
- Initial tests passed.
- Product and ProductVersion domain entities created.
- TestSuite and TestSuiteRevision connected to Product.
- TestTemplate, TemplateRevision, TemplateSection, TestCaseTemplate, and TestStepTemplate created.
- Tag and EntityTag domain entities created.
- Domain test coverage expanded to product/version, test suite/revision, template hierarchy, and tags.
- Application read models and test suite catalog interface created.
- Infrastructure sample data service created.
- First clickable read-only Templates screen created.
- Infrastructure test coverage added for sample catalog hierarchy.
- Test suite revisions are optional in the model.
- Sample data includes a suite where revision is not required.
- CustomFieldDefinition and CustomFieldValue domain entities created.
- Attachment domain entity created.
- EntityLink domain entity created.
- EntityReferenceType introduced for tags, links, attachments, and future references.
- Domain test coverage expanded for dynamic fields, attachments, and links.
- EF Core SQLite package connected to Infrastructure.
- SQLite DbContext and persistence records created.
- Local database initializer created.
- Database seed flow created from sample catalog data.
- Templates screen now reads through a SQLite-backed catalog service.
- Infrastructure test coverage expanded for database creation, seeding, and SQLite-backed reads.
- Application write contracts created for test suite and section management.
- SQLite-backed test suite management service created.
- Templates screen can create a test suite with optional revision support.
- Templates screen can create a section for the selected test suite/revision.
- Infrastructure test coverage expanded for test suite creation, optional revision sections, and validation.
- Templates screen can create a test case for the selected section.
- Templates screen can create a check for the selected test case.
- Infrastructure test coverage expanded for full section, case, and check creation.
- Templates screen now opens add forms in a popup dialog instead of permanent inline forms.
- Revision column is hidden when the selected test suite does not require revisions.
- Templates screen can delete selected test suites, sections, test cases, and checks after confirmation.
- Infrastructure test coverage expanded for check deletion and section cascade deletion.
- Templates screen can edit existing test suites, sections, test cases, and checks.
- Row-level Edit and Delete actions appear on hover for the item being changed.
- Test suite deletion is labeled as Delete all because it removes the suite hierarchy.
- Infrastructure test coverage expanded for template hierarchy updates.
- Template list rows now stretch to the visible width and disable horizontal scrolling.
- Fields page added for user-defined dynamic field definitions.
- Dynamic fields can be created for templates, sessions, results, and bug reports.
- Dynamic fields can be scoped globally or to a selected test suite, section, or test case.
- Dynamic field definitions can be edited and re-scoped.
- Dynamic field definitions can be archived.
- Dynamic field definitions can be deleted with a confirmation popup that warns about losing saved values.
- SQLite persistence added for custom field definitions.
- SQLite persistence added for custom field values.
- Infrastructure test coverage expanded for dynamic field definition create/archive/scope workflows.
- Test Sessions page added.
- Manual test sessions can be created from an existing test suite/revision.
- Creating a session copies sections, test cases, and checks into result records.
- New copied test case and check results start as Not Tested.
- Test Sessions page can show selected session details with copied sections, cases, and checks.
- Test Sessions page shows checks inside their parent test case.
- Testers can update test case and check statuses.
- Testers can save result comments for test cases and checks.
- Updating a failed check automatically marks the parent test case as failed.
- Testers can add attachment evidence files to test case and check results.
- Testers can open and delete attachment evidence files from the result dialog.
- Image attachments show a small preview in the result dialog.
- Test Sessions page shows a selected session status summary.
- Test Sessions page can filter visible test cases by result status.
- SQLite persistence added for test sessions, section results, case results, and check results.
- SQLite persistence added for attachment metadata.
- Local file storage added for attachment evidence files.
- Infrastructure test coverage expanded for test session creation, result updates, attachment evidence add/delete, and revision validation.
- Demo script documentation added for work presentation.
- Release checklist documentation added for build/test/publish steps.
- Bugs page added with first create/list/status update workflow.
- SQLite persistence added for bug reports.
- Infrastructure test coverage expanded for bug creation and status updates.
- Bug creation now rejects duplicate bug titles.
- Modular error dialog service added for popup errors.
- Test case/check result dialog can create a linked bug report.
- Bugs page shows the linked test item for linked bugs.
- Popup errors now use a modular MahApps.Metro dialog service instead of classic Windows message boxes.
- Bugs page supports tester/developer comments for the selected bug.
- Bugs page supports attachments for the selected bug.
- Attachment picker now accepts screenshots, videos, logs, scripts, documents, and all file types.
- SQLite persistence added for generic discussion comments.
- Infrastructure test coverage expanded for bug comments and bug video attachments.
- Bugs page opens the selected bug in a right-side detail drawer for review and correction.
- Bugs page opens discussion in a compact right-side drawer instead of using permanent screen space.
- Test case and check results can open their own discussion drawer.
- Discussion messages can be added, edited, deleted, and show created/edited timestamps.
- Bugs page can filter by status, severity, and priority.
- Test Sessions page can create a manual session without a template.
- Test Sessions page hides the revision picker when the selected template source does not require revisions.
- Test Sessions page can add manual sections, test cases, and checks directly inside a selected session.
- New bug creation shows active bug custom fields before the bug is saved.
- Required custom fields are validated when creating a bug or saving field values.
- Custom field values can be saved for bug reports.
- Custom field values can be saved for test case and check results.
- Scoped custom fields appear for matching test suites, sections, and test cases.
- A shared WPF custom field editor is used by bug details and test result dialogs.
- Projects now separate templates, sessions, bugs, and custom fields into workspaces.
- Test suite names are unique only inside one project, so different projects can reuse the same template name.
- Whole projects can be deleted after a dangerous-action confirmation, including related templates, sessions, bugs, fields, attachments, and discussions.
- Test sessions can be deleted after confirmation, including related result data, custom field values, attachments, and discussions.
- New Test Session creation now opens in a popup dialog instead of permanently occupying the Test Sessions screen.
- Test Sessions page can create a new clean session by copying the structure of a previous session.
- Reports stage started with a modular report data service for full test session report data.
- Reports page can prepare a test session report preview from the report data model.
- Reports page can export the selected test session report to a first PDF file through a replaceable report export interface.
- Test session reports now include Last Status Change Date for test cases and checks.
- Report date-only values are formatted without time for cleaner management reports.
- Test session PDF export can embed image attachments safely when the image file exists.
- Test session PDF export can embed image attachments from both test cases and checks.
- Reports preview shows image attachment thumbnails for the selected test case card.
- Report check tables no longer place attachment file names inside the Check column.
- User-facing test wording now uses Test details instead of Expected result in templates, test sessions, and reports.
- Report status cells use color cues, check date columns use Test date wording, and custom fields no longer show a type column.
- Test result and bug detail editing now opens as centered modal dialogs instead of right-side edit panels.
- Reports can export linked bugs only when the user explicitly enables Include Linked Bugs in Report.
- Test case rows in PDF reports now use the same tabular style as checks.
- Report custom fields have clearer spacing around their table.
- PDF report table fields now protect against overly long text by wrapping long tokens instead of stretching columns.
- PDF report long tokens now use invisible wrap opportunities so text follows column width without visible inserted spaces.
- PDF report case/check status is shown as the last table column.
- PDF report custom fields are shown as columns inside the test case/check tables instead of a separate custom field block.
- PDF report test case/check tables now use consistent width and include clear table headers.
- Experimental report branch adds an Attachments column with image thumbnails inside test case/check tables.
- Experimental report branch calculates comment text wrapping from the actual Comment column width.
- Test case and check result custom fields are limited to 3 fields per selected target.
- Bug and test chats use a shared modern drawer style with distinct own-message bubbles.
- Test Sessions page shows a clear empty state when no sessions exist.
- Linked bug creation in Test Sessions opens in a modal instead of permanently occupying the result details panel.
- Test case and check result cards show linked bug indicators when bugs already exist.
- Linked bug modal shows existing linked bugs before creating another linked bug.
- Selectable cards started moving to a shared selectable style with visible background, hover, and selected states.
- Test Sessions page can preview saving session structure back to a template.
- Test Sessions page can append manual sections, cases, and checks back to the original template after explicit user action.
- Test Sessions page can create a new template from the current session structure.
- Template sync actions show a success confirmation dialog after saving.
- Templates screen refreshes its catalog when the user opens it, so synced/created templates are visible.
- Test case and check results store a read-only Last Status Change Date.
- Last Status Change Date updates only when the result status actually changes, not when only comments change.
- Required result custom fields are highlighted in the test result dialog when they are empty.
- Saving a test case/check result is blocked with a clear popup when required custom fields are missing.
- Existing test sessions pick up newly added matching required custom fields when a result is opened.
- Test suite revisions can be created, renamed, and copied from an existing revision without modifying the original revision.
- Test suite revision requirement can be disabled again while editing a test suite.
- Custom fields can be bound to multiple selected scopes/targets without duplicating the same field definition.
- Discussion unread indicators were added for bug, test case, and check chat buttons.

## Product Goal

Build a Windows desktop application for managing manual test templates, manual test session reports, bug reports, images, audit history, filtering, tags, versioning, and PDF exports.

The application must replace the current Excel-based workflow where each application version requires manually rewriting test evidence and reports.

## Confirmed Decisions

- Target platform: Windows desktop.
- Language and UI stack: C#, .NET, WPF, MVVM.
- UI library: MahApps.Metro.
- Repository language: English.
- Application UI text: English.
- Code comments: English.
- Conversation language with the project owner: Russian.
- Users: more than one person will use the system. At minimum, tester and developer roles are expected.
- Users must have their own accounts and roles in the future.
- Users must be able to sign in from more than one PC, not only from their own Windows machine.
- The first demo may use Windows user name, but the architecture must be ready for real shared authentication.
- First demo should use local storage, but the architecture must be ready for server storage.
- Future production target should support shared storage on a company server.
- Audit tracking should use the Windows user name in the first version.
- Roles should exist in the data model even if authentication is simple at first.
- Tests and bugs must support dynamic fields.
- Each team must be able to define which fields are required and which fields are optional.
- Test suite revision must be optional because many checks are based on windows, controls, firmware workflows, or UI areas without formal revisions.
- Test templates are recommended for repeatable work, but manual test sessions must also support creating tests directly without a template.
- New manual test sessions should be creatable from a template, from a previous session, or from an empty manual session.
- Test structure must support user-defined test suites, revisions such as Revision A and Revision B, sections such as Normal and Abnormal, test cases, checks, dates, versions, tags, and attachments.
- Reports must include information per test suite, revision, section, application tab/window, sub-tab, button, table, graph, date field, model, firmware, status, comments, images, and other details.
- PDF report generation is mandatory.
- PDF reports should start from the current Excel report idea but become cleaner and easier to read.
- Future report architecture must support additional report types such as Bug Report and Fix Report.
- Attachments and photos are mandatory.
- Attachments must support screenshots, photos, videos, text files, logs, scripts, documents, and future file types.
- Templates, manual sessions, bugs, comments, and results must support attachments/photos/files where useful.
- Copying templates or previous sessions for the next version is mandatory.
- Tags and links between related items are mandatory.
- Sorting, filtering, and tags are mandatory.
- Users must be able to filter by failed tests, unfinished bugs, version, revision, section, category, tags, status, owner, and date.
- Developers should be able to add comments, change bug status such as Fixed, and edit bug-related information.
- Testers should be able to edit test results, add comments, add photos, and create bugs.
- Bugs must support a conversation/comment area for tester and developer communication.
- Test sessions/results should support a conversation/comment area when discussion is needed around a result.
- Popup errors should follow the app visual style and use MahApps.Metro dialogs, not classic Windows message boxes.
- Discussion/chat UI should not permanently take main screen space; it should open as a compact drawer or dialog from a chat action.
- Each bug, test case result, and check result should have its own discussion/chat thread.
- Chat messages should support created time, edit, and delete workflows.
- Bugs should open into a full detail view/drawer for review, scrolling, and correction.
- Application versioning, builds, future updates, and release control must be planned from the beginning.
- Final app should be publishable as a Windows .exe.

## Critical Open Questions

These are still important, but they do not block the first skeleton:

1. Should the first demo database be local SQLite or SQL Server LocalDB?
2. Should bugs be linkable to a specific check, only to a test case, or both?
3. What exact roles are needed at first: Admin, Tester, Developer, Viewer?
4. Should reports include a signature/approval section?
5. Should attachments be stored in a shared folder on the server in production?
6. Should the app support English only, or should the architecture leave room for more languages later?

## Must Not Forget

- Internally rename the technical Step/TestStep naming to Check/TestCheck after the workflow is stable. The UI already uses Check, but the C# model still contains old Step names to avoid a risky rename while behavior is changing quickly.
- Keep the storage layer ready for a future shared database/server deployment. Local SQLite is only the first demo storage, and production should be able to move to a shared SQL database and shared attachment folder without rewriting the UI.
- Keep each feature modular so test templates, sessions, bugs, attachments, reports, and audit history can be changed independently.
- Continue improving direct manual test creation inside Test Sessions as real workflows become clearer.
- Do not force every test session to start from a template; keep the model ready for template-based and free-form testing.
- Keep business logic ready for a future web UI by avoiding WPF dependencies outside the App project.
- Later, add a dedicated navigation/view composition layer: `NavigationService`, optional `ViewModelFactory`, app-level service registration extensions, and possibly a `ViewLocator` so view-model-to-view mapping stays modular and easy to replace.
- Later, add an analytics/dashboard screen with a table and charts for filtering test and bug data by date range, status, version, build, owner, and other report fields.
- Later, add application logging for important user actions and errors.
- Later, add an activity/event panel for visible app events such as template creation, status changes, project deletion, and important errors.
- Later, add full theme support with Dark Mode, a theme switcher, and saved theme preference.
- Continue extending session-to-template sync workflows beyond the first safe structure-only version.
- Later, extend session-to-template sync to include supported custom field definition changes when field editing becomes available inside the testing workflow.
- Later, add an optional bulk sync/review action for required fields added after sessions already exist.
- Later, add Created Date and Last Modified Date to session result items.
- Later, support multi-session reports and full-project/program reports with image attachments embedded in the PDF.

## Proposed Technology

- Runtime target for the first skeleton: .NET 9 on this machine.
- Future runtime target: .NET 10 LTS after the SDK is installed.
- UI: WPF.
- UI styling: MahApps.Metro.
- Pattern: MVVM.
- MVVM helpers: CommunityToolkit.Mvvm.
- Data access: Entity Framework Core.
- Demo database option: SQLite or SQL Server LocalDB.
- Production database option: SQL Server Express/Standard on a company server.
- Attachment storage: file storage with metadata in the database.
- PDF export: choose after checking license and report requirements.
- Tests: xUnit or NUnit.
- Source control: Git and GitHub.

## Proposed Solution Structure

```text
BugTestManager.sln
README.md
AGENTS.md
TASKS.md
.gitignore
docs/
  architecture.md
  data-model.md
  decisions/
    0001-technology-stack.md
    0002-storage-strategy.md
src/
  BugTestManager.App/
    Views/
    ViewModels/
    Controls/
    Resources/
    Converters/
    Services/
  BugTestManager.Domain/
    Entities/
    Enums/
    ValueObjects/
  BugTestManager.Application/
    Abstractions/
    DTOs/
    Services/
    UseCases/
    Validation/
  BugTestManager.Infrastructure/
    Data/
    FileStorage/
    Pdf/
    Repositories/
    Migrations/
tests/
  BugTestManager.Domain.Tests/
  BugTestManager.Application.Tests/
```

## Architecture Rules

- Keep UI code in the WPF project.
- Keep business entities and rules in the Domain project.
- Keep use cases and interfaces in the Application project.
- Keep database, file storage, PDF, and external implementation details in the Infrastructure project.
- Do not put database code directly in ViewModels.
- Do not put UI code in Domain or Application projects.
- Do not hardcode secrets, passwords, server names, or personal paths.
- Keep all UI text in English.
- Keep all code comments in English.
- Keep commits small and clear.

## Beginner Notes

- WPF is the desktop UI technology.
- MVVM means the screen is separated from the logic behind the screen.
- Entity Framework Core helps C# work with a database.
- A template is the reusable test plan.
- A test session is a manual QA report/snapshot used for one specific tested version or build. It does not mean the app runs automated tests.
- A test suite is a user-defined group of tests. It can represent a standard, window, product area, feature group, or any other test base.
- A revision is a version of a test suite or template, for example Revision A or Revision B.
- Revision is optional. Some test suites need revisions, and some do not.
- A section is a logical group inside a revision, for example Normal or Abnormal.
- A bug report is a tracked problem found during testing.
- An attachment is a photo, screenshot, log file, or document connected to a test or bug.
- Audit history means the app remembers who changed what and when.

## Data Model Direction

Recommended hierarchy:

```text
Product
  ProductVersion
    TestSuite
      TestSuiteRevision
        TestSession
          TestSection
            TestCaseResult
              TestStepResult
```

Templates:

```text
TestTemplate
  TemplateRevision
    TemplateSection
      TestCaseTemplate
        TestStepTemplate
```

Bugs:

```text
BugReport
  BugComment
  BugStatusHistory
  Attachment
  Tag
```

Dynamic fields:

- CustomFieldDefinition defines the field name, type, target entity, requirement flag, and options.
- CustomFieldValue stores the value for a specific test, bug, product version, test suite revision, section, or device.
- Requirement rules are user-defined, so one team can make firmware required while another team can leave it optional.
- Field types should include text, long text, number, date, date/time, checkbox, single select, multi select, and attachment reference.

Stable fields:

- Keep important common fields as normal database columns: title, status, owner, created date, updated date, created by, updated by.
- Use dynamic fields for company-specific details such as firmware, model, block type, special test values, or extra measurements.

Tags and filtering:

- Tags can be attached to tests, bugs, templates, revisions, and sections.
- Links can connect related tests, bugs, sessions, attachments, and future entities.
- Filters must support status, version, revision, section, category, tag, owner, date, failed tests, and unfinished bugs.

Attachments:

- Store files outside the database.
- Store metadata in the database.
- Use a shared attachment folder in production.
- Attachments must support screenshots, photos, logs, documents, and future file types.

History:

- Record create, update, delete, status change, attachment added, and report generated actions.
- Store Windows user name, role, timestamp, entity type, entity id, action, and changed values.

## Milestones

### Milestone 0 - Planning and Repository Setup

- Finalize open questions.
- Create README.md.
- Create AGENTS.md.
- Create docs folder.
- Create architecture notes.
- Create data model notes.
- Create initial GitHub repository structure.

### Milestone 1 - Application Skeleton

- Create WPF solution. Done.
- Add MahApps.Metro. Done.
- Add MVVM structure. Done.
- Add dependency injection. Done.
- Add basic navigation shell. Done.
- Add local configuration.
- Add database setup placeholder. Done.

### Milestone 2 - Core Data and Audit

- Add products and product versions. Started.
- Add test suites and test suite revisions. Started.
- Add users/roles model.
- Add Windows user name tracking.
- Add audit log.
- Add attachment metadata. Started at Domain level.
- Add tags and basic filtering contracts. Started.
- Add linking contracts between related entities. Started at Domain level.

### Milestone 3 - Templates and Dynamic Fields

- Create test template management. Started with catalog browsing and first create workflow.
- Add dynamic field definitions. Started at Domain level.
- Add dynamic field definition management screen. Started.
- Add template revisions, sections, cases, and steps. Started at Domain level.
- Add copy-from-template workflow.
- Add copy-from-previous-session workflow. Done.
- Replace in-memory sample catalog with database-backed reads. Done.
- Add first test suite and section creation workflow. Done.
- Add first test case and check creation workflow. Done.
- Add delete confirmation workflow for template hierarchy items. Done.
- Add edit workflow for existing template hierarchy items. Done.

### Milestone 4 - Test Execution

- Create manual test sessions from templates. Started.
- Create manual test sessions without templates.
- Add manual section, test case, and check creation inside an active test session.
- Add statuses: Not Tested, Pass, Fail, Blocked, Not Applicable.
- Add comments, dates, model, firmware, and custom fields.
- Add custom field value editing for test case and check results. Started.
- Add photo/file attachments.
- Add result discussion/comments for tester/developer communication.
- Add filtering by status, revision, section, category, tags, owner, and dates.

### Milestone 5 - Bug Tracker

- Create bug report management.
- Add bug workflow.
- Add severity and priority.
- Link bugs to test cases and checks.
- Add bug comments, attachments, and history.
- Add bug conversation/comments for tester/developer communication.
- Add bug attachments for screenshots, videos, logs, scripts, and documents.
- Add custom field value editing for bug reports. Started.
- Add filtering by status, priority, severity, owner, tags, version, and date.

### Milestone 6 - PDF Reports

- Prepare full test session report data. Started.
- Generate full test report PDF. Started.
- Generate bug report PDF.
- Generate summary/sign-off report if required.
- Include images, metadata, dates, statuses, custom fields, and summary tables.
- Format report-only dates without time unless the field is explicitly date/time.
- Add multi-session report export.
- Add project/program-level report export.
- Embed image attachments safely with scaling and missing-file handling.

### Milestone 7 - Packaging and Release

- Publish as Windows .exe.
- Add app settings for database and attachment storage.
- Add release documentation.
- Add GitHub workflow for build/test if useful.

### Milestone 8 - Future Work

- Excel import.
- Shared server deployment.
- Advanced search.
- Dashboard with filterable tables, charts, version statistics, test status statistics, bug status statistics, and date range filtering.
- Auto-update.
- More languages.

## Current Stage

The project is between Milestone 4 and Milestone 5.

Milestone 1 created the WPF/MVVM skeleton and basic navigation.

Milestone 2 has started with the first Domain entities and SQLite persistence.

Milestone 3 has started with a Templates screen using database-backed seeded data.

Milestone 4 has started with manual test sessions, result status editing, result comments, attachments, discussions, filters, manual session items, and custom field values for result items.

Milestone 5 has started with bug creation, duplicate-title validation, status updates, filters, attachments, detail drawers, discussions, linked bugs, and custom field values for bug reports.

## Next Step

Manually test the new project/session/revision/field workflows in the WPF app, then continue with copy-from-previous-session and report/dashboard planning.
