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

The initial application skeleton has been created. It includes the WPF app, layered projects, MahApps.Metro setup, MVVM starter structure, first Domain entities, and a first Domain test project.

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

Current Domain tests cover product versions, test suites, revisions, template hierarchy, tags, and basic validation rules.

## Development Rules

- Keep the application UI in English.
- Keep code comments in English.
- Do not hardcode secrets, passwords, or personal paths.
- Keep commits focused and readable.
- Prefer simple architecture that can grow later.
- Verify changes with build and tests whenever possible.
