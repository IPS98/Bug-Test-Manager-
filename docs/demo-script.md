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
15. Open or delete the attachment from the result dialog.
16. Go to `Bugs`.
17. Create a bug report.
18. Change the bug status to `Fixed` or `Ready for Retest`.
19. Try creating another bug with the same title and show the duplicate-title popup.
20. Return to `Test Sessions`, open a failed case/check, and create a linked bug from the result dialog.
21. Select the linked bug in `Bugs`.
22. Open the bug details drawer by double-clicking the bug or pressing `Open`.
23. Correct bug details and save.
24. Open the bug discussion drawer with `Chat`.
25. Add, edit, and delete a tester/developer message.
26. Add a screenshot, video, log, script, or document in the bug details attachment area.
27. Return to `Test Sessions` and open `Chat` on a test case or check.

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
- Attachment open/delete support.
- Small image preview for image attachments.
- First bug tracker workflow.
- Linked bug creation from a test case or check.
- Duplicate bug title validation with popup feedback.
- MahApps.Metro styled error dialogs.
- Compact discussion drawer for bugs, test cases, and checks.
- Discussion messages support add, edit, delete, created time, and edited time.
- Bug details drawer for reviewing and correcting a bug.
- Bug attachments for screenshots, videos, logs, scripts, and documents.
- Automated tests for core persistence workflows.

## Known Limitations

- UI polish is still early.
- Internal code still uses some `Step` naming while the UI uses `Check`.
- Attachment handling is still basic and needs stronger preview, validation, and reporting integration.
- Direct test session creation without a template is planned.
- PDF reports are still planned.
- Shared server database setup is planned, but the current demo uses local SQLite.

## Recommended Demo Message

The current version is not the final product. It is a working prototype that proves the architecture, storage, template creation, manual test execution, evidence attachment, and automated verification direction.
