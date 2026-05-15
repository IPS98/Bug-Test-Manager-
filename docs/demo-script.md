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
9. Create another test session with `Start without template`.
10. Add a manual section, manual test case, and manual check inside that session.
11. Select the template-based session or the manual session.
12. Open a test case with checks.
13. Mark one check as `Fail`.
14. Show that the parent test case becomes `Fail`.
15. Use the result filter to show failed items.
16. Add an attachment to a test case or check.
17. Open or delete the attachment from the result dialog.
18. Go to `Bugs`.
19. Create a bug report.
20. Change the bug status to `Fixed` or `Ready for Retest`.
21. Filter bugs by status, severity, and priority.
22. Try creating another bug with the same title and show the duplicate-title popup.
23. Return to `Test Sessions`, open a failed case/check, and create a linked bug from the result dialog.
24. Select the linked bug in `Bugs`.
25. Open the bug details drawer by double-clicking the bug or pressing `Open`.
26. Correct bug details and save.
27. Open the bug discussion drawer with `Chat`.
28. Add, edit, and delete a tester/developer message.
29. Add a screenshot, video, log, script, or document in the bug details attachment area.
30. Return to `Test Sessions` and open `Chat` on a test case or check.

## What Works Now

- WPF desktop shell with MVVM.
- SQLite local database.
- Test suite/template hierarchy.
- Optional revisions.
- Dynamic field definitions and scope selection.
- Manual test sessions copied from templates.
- Manual test sessions created without templates.
- Manual sections, test cases, and checks created directly inside a session.
- Test case and check status editing.
- Automatic parent case status update when a check fails.
- Basic attachment evidence storage.
- Attachment open/delete support.
- Small image preview for image attachments.
- First bug tracker workflow.
- Linked bug creation from a test case or check.
- Duplicate bug title validation with popup feedback.
- MahApps.Metro styled error dialogs.
- Compact discussion drawer for bugs, test cases, and checks.
- Discussion messages support add, edit, delete, created time, and edited time.
- Bug details drawer for reviewing and correcting a bug.
- Bug filters by status, severity, and priority.
- Bug attachments for screenshots, videos, logs, scripts, and documents.
- Automated tests for core persistence workflows.

## Known Limitations

- UI polish is still early.
- Internal code still uses some `Step` naming while the UI uses `Check`.
- Attachment handling is still basic and needs stronger preview, validation, and reporting integration.
- Custom field definitions are created, but custom field values are not connected to edit forms yet.
- PDF reports are still planned.
- Shared server database setup is planned, but the current demo uses local SQLite.

## Recommended Demo Message

The current version is not the final product. It is a working prototype that proves the architecture, storage, template creation, manual test execution, evidence attachment, and automated verification direction.
