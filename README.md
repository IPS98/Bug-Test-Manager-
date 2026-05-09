# Bug & Test Manager

Bug & Test Manager is a planned Windows desktop application for managing manual test templates, manual test sessions, bug reports, attachments, audit history, and PDF reports.

The project is intended to replace an Excel-based QA workflow where testers manually rewrite test reports for every application version.

## Planned Stack

- C#
- .NET
- WPF
- MVVM
- MahApps.Metro
- Entity Framework Core
- SQLite or SQL Server for data storage
- GitHub for source control

## Main Goals

- Create reusable test templates.
- Create manual test sessions from templates or previous sessions.
- Track test results per application version, tab, feature, case, and step.
- Track bugs in the same application.
- Store screenshots and other attachments.
- Keep audit history of user changes.
- Export clear PDF reports.
- Publish the application as a Windows .exe.

## Repository Structure

```text
docs/    Project documentation and architecture notes.
src/     Application source code.
tests/   Automated tests.
```

The initial application skeleton has been created. It includes the WPF app, layered projects, MahApps.Metro setup, MVVM starter structure, first Domain entities, a clickable template browser, first template hierarchy creation workflow, SQLite persistence, seed data, and test projects.

## Documentation

- [Tasks](TASKS.md)
- [Architecture](docs/architecture.md)
- [Data Model](docs/data-model.md)

## Build

```powershell
dotnet build BugTestManager.sln
```

## Test

```powershell
dotnet test BugTestManager.sln
```

Current tests cover product versions, test suites, optional revisions, template hierarchy, tags, dynamic fields, attachments, links, sample catalog data, SQLite database creation, seeding, database-backed reads, first SQLite-backed template hierarchy create workflows, and basic validation rules.

## Local Data

The demo app creates a local SQLite database here:

```text
%LOCALAPPDATA%\BugTestManager\BugTestManager.db
```

Local database files are ignored by Git.

## Development Rules

- Keep the application UI in English.
- Keep code comments in English.
- Do not hardcode secrets, passwords, or personal paths.
- Keep commits focused and readable.
- Prefer simple architecture that can grow later.
- Verify changes with build and tests whenever possible.
