# Bug & Test Manager - Tasks

Status: planning and approved skeleton setup. Application code may start with the project skeleton only.

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
- First demo should use local storage, but the architecture must be ready for server storage.
- Future production target should support shared storage on a company server.
- Audit tracking should use the Windows user name in the first version.
- Roles should exist in the data model even if authentication is simple at first.
- Tests and bugs must support dynamic fields.
- Test templates are required. New manual test sessions should be created from previous templates or previous sessions.
- Test structure must support user-defined test suites, revisions such as Revision A and Revision B, sections such as Normal and Abnormal, test cases, test steps, dates, versions, tags, and attachments.
- Reports must include information per test suite, revision, section, application tab/window, sub-tab, button, table, graph, date field, model, firmware, status, comments, images, and other details.
- PDF report generation is mandatory.
- PDF reports should start from the current Excel report idea but become cleaner and easier to read.
- Attachments and photos are mandatory.
- Sorting, filtering, and tags are mandatory.
- Users must be able to filter by failed tests, unfinished bugs, version, revision, section, category, tags, status, owner, and date.
- Developers should be able to add comments, change bug status such as Fixed, and edit bug-related information.
- Testers should be able to edit test results, add comments, add photos, and create bugs.
- Application versioning, builds, future updates, and release control must be planned from the beginning.
- Final app should be publishable as a Windows .exe.

## Critical Open Questions

These are still important, but they do not block the first skeleton:

1. Should the first demo database be local SQLite or SQL Server LocalDB?
2. Should bugs be linkable to a specific test step, only to a test case, or both?
3. What exact roles are needed at first: Admin, Tester, Developer, Viewer?
4. Should reports include a signature/approval section?
5. Should attachments be stored in a shared folder on the server in production?
6. Should the app support English only, or should the architecture leave room for more languages later?

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
- Field types should include text, long text, number, date, date/time, checkbox, single select, multi select, and attachment reference.

Stable fields:

- Keep important common fields as normal database columns: title, status, owner, created date, updated date, created by, updated by.
- Use dynamic fields for company-specific details such as firmware, model, block type, special test values, or extra measurements.

Tags and filtering:

- Tags can be attached to tests, bugs, templates, revisions, and sections.
- Filters must support status, version, revision, section, category, tag, owner, date, failed tests, and unfinished bugs.

Attachments:

- Store files outside the database.
- Store metadata in the database.
- Use a shared attachment folder in production.

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
- Add database setup placeholder.

### Milestone 2 - Core Data and Audit

- Add products and product versions. Started.
- Add test suites and test suite revisions. Started.
- Add users/roles model.
- Add Windows user name tracking.
- Add audit log.
- Add attachment metadata.
- Add tags and basic filtering contracts. Started.

### Milestone 3 - Templates and Dynamic Fields

- Create test template management. Started with read-only sample browser.
- Add dynamic field definitions.
- Add template revisions, sections, cases, and steps. Started at Domain level.
- Add copy-from-template workflow.

### Milestone 4 - Test Execution

- Create manual test sessions from templates.
- Add statuses: Not Tested, Pass, Fail, Blocked, Not Applicable.
- Add comments, dates, model, firmware, and custom fields.
- Add photo/file attachments.
- Add filtering by status, revision, section, category, tags, owner, and dates.

### Milestone 5 - Bug Tracker

- Create bug report management.
- Add bug workflow.
- Add severity and priority.
- Link bugs to test cases and test steps.
- Add bug comments, attachments, and history.
- Add filtering by status, priority, severity, owner, tags, version, and date.

### Milestone 6 - PDF Reports

- Generate full test report PDF.
- Generate bug report PDF.
- Generate summary/sign-off report if required.
- Include images, metadata, dates, statuses, custom fields, and summary tables.

### Milestone 7 - Packaging and Release

- Publish as Windows .exe.
- Add app settings for database and attachment storage.
- Add release documentation.
- Add GitHub workflow for build/test if useful.

### Milestone 8 - Future Work

- Excel import.
- Shared server deployment.
- Advanced search.
- Dashboard.
- Auto-update.
- More languages.

## Current Stage

The project is between Milestone 2 and Milestone 3.

Milestone 1 created the WPF/MVVM skeleton and basic navigation.

Milestone 2 has started with the first Domain entities.

Milestone 3 has started with a read-only Templates screen using sample in-memory data.

Database storage and editing workflows are not implemented yet.

## Next Step

Create the first persistence layer for templates and sample data, then replace the in-memory sample catalog with database-backed reads.
