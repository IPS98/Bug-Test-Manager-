# Demo Script

This script describes the current demo flow for Bug & Test Manager.

## Goal

Show that the project already has a real Windows desktop foundation for replacing Excel-based manual test reports.

## Demo Flow

1. Open the application.
2. Go to `Templates`.
3. Show that a test suite can contain sections, test cases, and checks.
4. Create or edit a small test suite item if needed.
5. Go to `Fields`.
6. Show that user-defined fields can be scoped globally or to a selected suite, section, or test case.
7. Go to `Test Sessions`.
8. Create a new test session from an existing test suite.
9. Select the created session.
10. Open a test case with checks.
11. Mark one check as `Fail`.
12. Show that the parent test case becomes `Fail`.
13. Use the result filter to show failed items.
14. Add an attachment to a test case or check.

## What Works Now

- WPF desktop shell with MVVM.
- SQLite local database.
- Test suite/template hierarchy.
- Optional revisions.
- Dynamic field definitions and scope selection.
- Manual test sessions copied from templates.
- Test case and check status editing.
- Automatic parent case status update when a check fails.
- Basic attachment evidence storage.
- Automated tests for core persistence workflows.

## Known Limitations

- UI polish is still early.
- Internal code still uses some `Step` naming while the UI uses `Check`.
- Attachments can be added and listed, but preview/open/delete is not implemented yet.
- Bug tracker is still a placeholder.
- PDF reports are still planned.
- Shared server database setup is planned, but the current demo uses local SQLite.

## Recommended Demo Message

The current version is not the final product. It is a working prototype that proves the architecture, storage, template creation, manual test execution, evidence attachment, and automated verification direction.
