# Architecture

## Purpose

Bug & Test Manager is a Windows desktop application for QA test management, bug tracking, attachments, audit history, filtering, tags, versioning, and PDF reporting.

The application should start simple enough for a working demo, but the architecture must support future shared server storage, multiple users, filtering, tags, versioning, and release updates.

## Architecture Style

The proposed architecture is a layered desktop application:

```text
WPF UI
  -> Application use cases
    -> Domain model
    -> Infrastructure implementations
```

## Projects

### BugTestManager.App

WPF application project.

Responsibilities:

- Windows and dialogs.
- Views and user controls.
- ViewModels.
- Navigation.
- MahApps.Metro styling.
- User interaction.

This project can reference Application and Infrastructure.

### BugTestManager.Domain

Business model project.

Responsibilities:

- Core entities.
- Enums.
- Value objects.
- Business rules that do not depend on WPF or the database.

This project should not reference WPF, Entity Framework, or file system implementation details.

### BugTestManager.Application

Use case project.

Responsibilities:

- Application services.
- Use cases.
- DTOs.
- Validation.
- Interfaces for persistence, file storage, PDF export, filtering, and user context.

This project defines what the app can do without deciding how the database or PDF library works internally.

### BugTestManager.Infrastructure

Implementation project.

Responsibilities:

- Entity Framework Core DbContext.
- Database migrations.
- Repository implementations.
- Attachment file storage.
- PDF export implementation.
- Configuration loading.

## Storage Strategy

The first demo can use local storage, but the design should prepare for shared server storage.

Recommended direction:

- Demo mode: SQLite or SQL Server LocalDB.
- Production mode: SQL Server on a company server.
- Attachments: local folder in demo mode, shared folder in production mode.

The Application layer should depend on interfaces, not directly on SQLite or SQL Server.

## Test Organization

The test model must be more flexible than a simple screen/tab list.

The planned structure is:

```text
Product
  ProductVersion
    TestSuite
      TestSuiteRevision
        TestSession
          TestSection
            TestCase
              TestStep
```

Example:

```text
Power Supply App
  Version 2.5.0
    User-defined test suite
      Revision A
        Manual session for build 2.5.0.104
          Normal
            Input Voltage Test
              Step 1
              Step 2
          Abnormal
            Overload Test
```

This allows the app to store any user-defined test suite, optional revisions such as A/B, sections such as Normal/Abnormal, detailed tests, steps, dates, statuses, attachments, and dynamic data.

## User Tracking

Version 1 should use the Windows user name to record who created or changed records.

The data model should still include roles:

- Admin
- Tester
- Developer
- Viewer

This allows simple audit tracking now and more complete authentication later.

## Roles and Permissions

Initial permission direction:

- Testers can edit test results, comments, custom fields, attachments, and create bugs.
- Developers can edit bug comments, change bug statuses such as Fixed, add technical notes, and update assigned bug fields.
- Admins can manage templates, dynamic fields, roles, and configuration.
- Viewers can read reports and data without editing.

The exact permission system can start simple, but the code should not assume that all users can do everything.

## Dynamic Fields

Tests and bugs need flexible fields because every tested screen, test suite, revision, or device can require different information.

The app should support:

- Field definitions.
- Field types.
- Required/optional fields.
- Select-list options.
- Values connected to specific records.

Common stable data should still be normal database columns. Dynamic fields should be used for company-specific and changing details.

Required fields must be configurable per target/template. One team may require firmware and model, while another team may require only date, status, and screenshot.

## Attachments and Links

Attachments are a core feature. Tests, sessions, bugs, comments, and results should support screenshots, photos, logs, documents, and future file types.

The model should also support links between related items:

- Bug to test case.
- Bug to test step.
- Session to source template.
- Session to previous session.
- Report to generated source data.

This keeps the system flexible without hardcoding every possible relationship.

## Sorting, Filtering, and Tags

Filtering is a core feature, not an optional extra.

The app should support filters for:

- Failed tests.
- Not tested tests.
- Blocked tests.
- Unfinished bugs.
- Product version.
- Test suite.
- Test suite revision.
- Section.
- Category.
- Tags.
- Owner.
- Date range.
- Severity and priority.

Tags should be supported on tests, bugs, templates, revisions, and sections.

## PDF Reports

PDF report generation is a core feature.

Reports should support:

- Full manual test session report.
- Bug report.
- Summary report.
- Attachments and screenshots.
- Custom fields.
- Status summaries.
- Test suite, revision, section, test case, and test step details.
- Audit/sign-off section if required.

The PDF library must be selected after checking license requirements.

## Versioning and Updates

The project should track:

- Application version.
- Build number.
- Database schema version through migrations.
- Release notes.
- Future update strategy.

The first version can be manually published as an .exe, but the architecture should not block future installers or auto-update.
