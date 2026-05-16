# Reports

Reports are a core product feature because test results must be readable by managers, developers, testers, and future auditors.

## Direction

The reporting module should be built in layers:

1. Report data collection.
2. Report layout model.
3. PDF rendering.
4. Export history and audit events.
5. Optional dashboards and charts.

The first implementation starts with report data collection. This keeps the database/report logic independent from the PDF library, so the renderer can be changed later without rewriting test-session logic.

## Report Data

A full test session report should include:

- project name;
- test session name;
- tested version and build number;
- test suite and optional revision;
- notes and created-by metadata;
- status summary;
- sections;
- test cases;
- checks;
- result comments;
- custom fields;
- attachments metadata;
- image attachments embedded in PDF when the file is available;
- linked bugs.

Date-only report values should be displayed without time. Date/time fields can still include time when the field type explicitly requires it.

## PDF Renderer Selection

The PDF renderer must be selected carefully because this application is intended for workplace use.

Selection criteria:

- license must be acceptable for company/commercial use;
- large reports must be supported;
- images/screenshots must be supported;
- tables and page headers/footers must be supported;
- the library should work well in a Windows desktop app;
- the library should be replaceable behind an application interface.

The current code should not depend directly on a specific PDF package from ViewModels.

Initial renderer decision:

- Use PDFsharp/MigraDoc through an `IReportExportService` Infrastructure implementation.
- Keep PDFsharp/MigraDoc outside WPF ViewModels.
- Keep the export contract in `Application`, so the renderer can be replaced later.
- Use the core package first. Image embedding and advanced layout can be improved behind the same interface.

## Future Report Types

- Full test session report.
- Bug report.
- Version/build summary report.
- Failed tests report.
- Unfinished bugs report.
- Sign-off report.

## Important Rule

PDF generation must use the report data model. It must not query the database directly from the WPF screen.
