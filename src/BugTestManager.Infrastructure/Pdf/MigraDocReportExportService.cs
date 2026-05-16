using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace BugTestManager.Infrastructure.Pdf;

public sealed class MigraDocReportExportService : IReportExportService
{
    private static readonly object FontSettingsLock = new();
    private static bool fontSettingsConfigured;

    public ReportExportResult ExportTestSessionReport(ExportTestSessionReportRequest request)
    {
        if (request.Report is null)
        {
            throw new ArgumentException("Report data is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutputFilePath))
        {
            throw new ArgumentException("Output file path is required.", nameof(request));
        }

        EnsureFontSettings();
        var outputDirectory = Path.GetDirectoryName(request.OutputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var document = BuildDocument(request.Report, request.GeneratedBy);
        var renderer = new PdfDocumentRenderer
        {
            Document = document
        };

        renderer.RenderDocument();
        renderer.PdfDocument.Save(request.OutputFilePath);

        return new ReportExportResult(request.OutputFilePath, DateTimeOffset.UtcNow);
    }

    private static Document BuildDocument(TestSessionReportItem report, string generatedBy)
    {
        var document = new Document();
        document.Info.Title = $"Test Session Report - {report.SessionName}";
        document.Info.Subject = "Bug & Test Manager test session report";
        document.Info.Author = string.IsNullOrWhiteSpace(generatedBy) ? report.CreatedBy : generatedBy;

        ConfigureStyles(document);

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.4);

        AddFooter(section);
        AddTitle(section, report);
        AddSessionMetadata(section, report, generatedBy);
        AddStatusSummary(section, report.Summary);
        AddSections(section, report.Sections);
        AddLinkedBugs(section, report.LinkedBugs);

        return document;
    }

    private static void ConfigureStyles(Document document)
    {
        var normal = GetStyle(document, StyleNames.Normal);
        normal.Font.Name = "Arial";
        normal.Font.Size = 9;

        var heading1 = GetStyle(document, StyleNames.Heading1);
        heading1.Font.Name = "Arial";
        heading1.Font.Size = 18;
        heading1.Font.Bold = true;
        heading1.Font.Color = Colors.DarkSlateBlue;
        heading1.ParagraphFormat.SpaceAfter = Unit.FromPoint(8);

        var heading2 = GetStyle(document, StyleNames.Heading2);
        heading2.Font.Name = "Arial";
        heading2.Font.Size = 13;
        heading2.Font.Bold = true;
        heading2.Font.Color = Colors.DarkSlateGray;
        heading2.ParagraphFormat.SpaceBefore = Unit.FromPoint(12);
        heading2.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);

        var heading3 = GetStyle(document, StyleNames.Heading3);
        heading3.Font.Name = "Arial";
        heading3.Font.Size = 10;
        heading3.Font.Bold = true;
        heading3.Font.Color = Colors.DarkSlateGray;
        heading3.ParagraphFormat.SpaceBefore = Unit.FromPoint(7);
        heading3.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
    }

    private static void EnsureFontSettings()
    {
        if (fontSettingsConfigured)
        {
            return;
        }

        lock (FontSettingsLock)
        {
            if (fontSettingsConfigured)
            {
                return;
            }

            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            fontSettingsConfigured = true;
        }
    }

    private static Style GetStyle(Document document, string styleName)
    {
        return document.Styles[styleName]
            ?? throw new InvalidOperationException($"MigraDoc style '{styleName}' was not found.");
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.Format.Font.Size = 8;
        footer.Format.Font.Color = Colors.Gray;
        footer.AddText("Page ");
        footer.AddPageField();
        footer.AddText(" of ");
        footer.AddNumPagesField();
    }

    private static void AddTitle(Section section, TestSessionReportItem report)
    {
        section.AddParagraph("Test Session Report", StyleNames.Heading1);
        var subtitle = section.AddParagraph(report.SessionName);
        subtitle.Format.Font.Size = 12;
        subtitle.Format.Font.Bold = true;
        subtitle.Format.SpaceAfter = Unit.FromPoint(8);
    }

    private static void AddSessionMetadata(Section section, TestSessionReportItem report, string generatedBy)
    {
        section.AddParagraph("Session Metadata", StyleNames.Heading2);
        var table = CreateTable(section, 4.2, 8.1, 4.2, 8.1);
        AddKeyValueRow(table, "Project", report.ProjectName, "Test suite", report.TestSuiteName);
        AddKeyValueRow(table, "Revision", string.IsNullOrWhiteSpace(report.TestSuiteRevisionName) ? "No revision" : report.TestSuiteRevisionName, "Version", EmptyAsDash(report.TestedVersion));
        AddKeyValueRow(table, "Build", EmptyAsDash(report.BuildNumber), "Created by", report.CreatedBy);
        AddKeyValueRow(table, "Created at", report.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), "Generated by", EmptyAsDash(generatedBy));

        if (!string.IsNullOrWhiteSpace(report.Notes))
        {
            var paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = Unit.FromPoint(6);
            paragraph.AddFormattedText("Notes: ", TextFormat.Bold);
            paragraph.AddText(report.Notes);
        }
    }

    private static void AddStatusSummary(Section section, ReportStatusSummaryItem summary)
    {
        section.AddParagraph("Status Summary", StyleNames.Heading2);
        var table = CreateTable(section, 4.1, 4.1, 4.1, 4.1, 4.1, 4.1);
        var header = table.AddRow();
        header.HeadingFormat = true;
        SetHeaderCell(header.Cells[0], "Total");
        SetHeaderCell(header.Cells[1], "Passed");
        SetHeaderCell(header.Cells[2], "Failed");
        SetHeaderCell(header.Cells[3], "Blocked");
        SetHeaderCell(header.Cells[4], "Not tested");
        SetHeaderCell(header.Cells[5], "N/A");

        var row = table.AddRow();
        row.Cells[0].AddParagraph(summary.Total.ToString());
        row.Cells[1].AddParagraph(summary.Passed.ToString());
        row.Cells[2].AddParagraph(summary.Failed.ToString());
        row.Cells[3].AddParagraph(summary.Blocked.ToString());
        row.Cells[4].AddParagraph(summary.NotTested.ToString());
        row.Cells[5].AddParagraph(summary.NotApplicable.ToString());
    }

    private static void AddSections(Section documentSection, IReadOnlyList<ReportSectionItem> sections)
    {
        documentSection.AddParagraph("Test Details", StyleNames.Heading2);

        foreach (var section in sections)
        {
            documentSection.AddParagraph($"{section.SortOrder}. {section.Name}", StyleNames.Heading2);
            if (!string.IsNullOrWhiteSpace(section.Category))
            {
                var category = documentSection.AddParagraph($"Category: {section.Category}");
                category.Format.Font.Color = Colors.Gray;
            }

            foreach (var testCase in section.TestCases)
            {
                AddTestCase(documentSection, testCase);
            }
        }
    }

    private static void AddTestCase(Section section, ReportTestCaseItem testCase)
    {
        section.AddParagraph($"{testCase.SortOrder}. {testCase.Title}", StyleNames.Heading3);

        var table = CreateTable(section, 4.2, 13.6, 3.2, 3.2);
        AddKeyValueRow(table, "Expected", EmptyAsDash(testCase.ExpectedResult), "Status", StatusText(testCase.Status));
        AddKeyValueRow(table, "Comment", EmptyAsDash(testCase.Comment), "Attachments", testCase.Attachments.Count.ToString());

        AddCustomFields(section, testCase.CustomFields);
        AddAttachments(section, testCase.Attachments);
        AddChecks(section, testCase.Checks);
    }

    private static void AddChecks(Section section, IReadOnlyList<ReportCheckItem> checks)
    {
        if (checks.Count == 0)
        {
            return;
        }

        var table = CreateTable(section, 1.2, 8.5, 8.5, 3.0, 4.0);
        var header = table.AddRow();
        header.HeadingFormat = true;
        SetHeaderCell(header.Cells[0], "#");
        SetHeaderCell(header.Cells[1], "Check");
        SetHeaderCell(header.Cells[2], "Expected");
        SetHeaderCell(header.Cells[3], "Status");
        SetHeaderCell(header.Cells[4], "Comment");

        foreach (var check in checks)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromPoint(3);
            row.BottomPadding = Unit.FromPoint(3);
            row.Cells[0].AddParagraph(check.SortOrder.ToString());
            row.Cells[1].AddParagraph(check.Text);
            row.Cells[2].AddParagraph(EmptyAsDash(check.ExpectedResult));
            row.Cells[3].AddParagraph(StatusText(check.Status));
            row.Cells[4].AddParagraph(EmptyAsDash(check.Comment));

            AddNestedList(row.Cells[1], "Fields", check.CustomFields.Select(field => $"{field.Name}: {field.Value}"));
            AddNestedList(row.Cells[1], "Attachments", check.Attachments.Select(attachment => attachment.OriginalFileName));
        }
    }

    private static void AddCustomFields(Section section, IReadOnlyList<ReportCustomFieldItem> customFields)
    {
        if (customFields.Count == 0)
        {
            return;
        }

        var paragraph = section.AddParagraph("Custom fields");
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceBefore = Unit.FromPoint(5);

        var table = CreateTable(section, 7.0, 13.0, 5.0);
        foreach (var field in customFields)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(field.Name);
            row.Cells[1].AddParagraph(EmptyAsDash(field.Value));
            row.Cells[2].AddParagraph(field.FieldType.ToString());
        }
    }

    private static void AddAttachments(Section section, IReadOnlyList<ReportAttachmentItem> attachments)
    {
        if (attachments.Count == 0)
        {
            return;
        }

        var paragraph = section.AddParagraph("Attachments");
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceBefore = Unit.FromPoint(5);

        var table = CreateTable(section, 8.0, 5.0, 8.0, 4.0);
        foreach (var attachment in attachments)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(attachment.OriginalFileName);
            row.Cells[1].AddParagraph(attachment.ContentType);
            row.Cells[2].AddParagraph(attachment.UploadedBy);
            row.Cells[3].AddParagraph(attachment.UploadedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        }
    }

    private static void AddLinkedBugs(Section section, IReadOnlyList<ReportBugItem> bugs)
    {
        section.AddParagraph("Linked Bugs", StyleNames.Heading2);
        if (bugs.Count == 0)
        {
            section.AddParagraph("No linked bugs.");
            return;
        }

        var table = CreateTable(section, 7.2, 3.0, 3.0, 3.0, 7.8);
        var header = table.AddRow();
        header.HeadingFormat = true;
        SetHeaderCell(header.Cells[0], "Title");
        SetHeaderCell(header.Cells[1], "Status");
        SetHeaderCell(header.Cells[2], "Severity");
        SetHeaderCell(header.Cells[3], "Priority");
        SetHeaderCell(header.Cells[4], "Linked item");

        foreach (var bug in bugs)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(bug.Title);
            row.Cells[1].AddParagraph(bug.Status);
            row.Cells[2].AddParagraph(EmptyAsDash(bug.Severity));
            row.Cells[3].AddParagraph(EmptyAsDash(bug.Priority));
            row.Cells[4].AddParagraph(EmptyAsDash(bug.LinkedEntityDisplayName));
        }
    }

    private static Table CreateTable(Section section, params double[] columnWidths)
    {
        var table = section.AddTable();
        table.Borders.Width = 0.25;
        table.Borders.Color = Colors.LightGray;
        table.Rows.LeftIndent = 0;
        table.Format.SpaceAfter = Unit.FromPoint(6);

        foreach (var width in columnWidths)
        {
            table.AddColumn(Unit.FromCentimeter(width));
        }

        return table;
    }

    private static void AddKeyValueRow(Table table, string firstKey, string firstValue, string secondKey, string secondValue)
    {
        var row = table.AddRow();
        SetLabelCell(row.Cells[0], firstKey);
        row.Cells[1].AddParagraph(firstValue);
        SetLabelCell(row.Cells[2], secondKey);
        row.Cells[3].AddParagraph(secondValue);
    }

    private static void SetHeaderCell(Cell cell, string text)
    {
        cell.Shading.Color = Colors.AliceBlue;
        cell.Format.Font.Bold = true;
        cell.AddParagraph(text);
    }

    private static void SetLabelCell(Cell cell, string text)
    {
        cell.Shading.Color = Colors.WhiteSmoke;
        cell.Format.Font.Bold = true;
        cell.AddParagraph(text);
    }

    private static void AddNestedList(Cell cell, string title, IEnumerable<string> items)
    {
        var materializedItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
        if (materializedItems.Count == 0)
        {
            return;
        }

        var heading = cell.AddParagraph();
        heading.Format.SpaceBefore = Unit.FromPoint(4);
        heading.AddFormattedText($"{title}: ", TextFormat.Bold);
        heading.AddText(string.Join(", ", materializedItems));
    }

    private static string StatusText(object status)
    {
        return status.ToString() switch
        {
            "NotTested" => "Not Tested",
            "NotApplicable" => "N/A",
            var value => value ?? "-"
        };
    }

    private static string EmptyAsDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
