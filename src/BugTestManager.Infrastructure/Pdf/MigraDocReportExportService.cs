using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace BugTestManager.Infrastructure.Pdf;

public sealed class MigraDocReportExportService : IReportExportService
{
    private const int MaxCustomFieldsPerReportItem = 3;
    private const int MaxReportTokenLength = 36;

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

        var document = BuildDocument(request.Report, request.GeneratedBy, request.IncludeLinkedBugs);
        var renderer = new PdfDocumentRenderer
        {
            Document = document
        };

        renderer.RenderDocument();
        renderer.PdfDocument.Save(request.OutputFilePath);

        return new ReportExportResult(request.OutputFilePath, DateTimeOffset.UtcNow);
    }

    private static Document BuildDocument(TestSessionReportItem report, string generatedBy, bool includeLinkedBugs)
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
        if (includeLinkedBugs)
        {
            AddLinkedBugs(section, report.LinkedBugs);
        }

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
        var subtitle = section.AddParagraph(ReportCellText(report.SessionName));
        subtitle.Format.Font.Size = 12;
        subtitle.Format.Font.Bold = true;
        subtitle.Format.SpaceAfter = Unit.FromPoint(8);
    }

    private static void AddSessionMetadata(Section section, TestSessionReportItem report, string generatedBy)
    {
        section.AddParagraph("Session Metadata", StyleNames.Heading2);
        var table = CreateTable(section, 4.2, 8.1, 4.2, 8.1);
        AddKeyValueRow(table, "Project", report.ProjectName, "Test suite", report.TestSuiteName);
        AddKeyValueRow(table, "Revision", string.IsNullOrWhiteSpace(report.TestSuiteRevisionName) ? "No revision" : report.TestSuiteRevisionName, "Version", report.TestedVersion);
        AddKeyValueRow(table, "Build", report.BuildNumber, "Created by", report.CreatedBy);
        AddKeyValueRow(table, "Created date", report.CreatedDateDisplay, "Generated by", generatedBy);

        if (!string.IsNullOrWhiteSpace(report.Notes))
        {
            var paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = Unit.FromPoint(6);
            paragraph.AddFormattedText("Notes: ", TextFormat.Bold);
            paragraph.AddText(ReportCellText(report.Notes));
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
        SetStatusValueCell(row.Cells[1], TestResultStatus.Pass);
        row.Cells[2].AddParagraph(summary.Failed.ToString());
        SetStatusValueCell(row.Cells[2], TestResultStatus.Fail);
        row.Cells[3].AddParagraph(summary.Blocked.ToString());
        SetStatusValueCell(row.Cells[3], TestResultStatus.Blocked);
        row.Cells[4].AddParagraph(summary.NotTested.ToString());
        SetStatusValueCell(row.Cells[4], TestResultStatus.NotTested);
        row.Cells[5].AddParagraph(summary.NotApplicable.ToString());
        SetStatusValueCell(row.Cells[5], TestResultStatus.NotApplicable);
    }

    private static void AddSections(Section documentSection, IReadOnlyList<ReportSectionItem> sections)
    {
        documentSection.AddParagraph("Test Details", StyleNames.Heading2);

        foreach (var section in sections)
        {
            documentSection.AddParagraph(ReportCellText($"{section.SortOrder}. {section.Name}"), StyleNames.Heading2);
            if (!string.IsNullOrWhiteSpace(section.Category))
            {
                var category = documentSection.AddParagraph(ReportCellText($"Category: {section.Category}"));
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
        section.AddParagraph(ReportCellText($"{testCase.SortOrder}. {testCase.Title}"), StyleNames.Heading3);

        var table = CreateTable(section, 1.1, 7.0, 7.0, 3.0, 4.8, 2.6);
        table.Format.SpaceAfter = Unit.FromPoint(5);

        var header = table.AddRow();
        header.HeadingFormat = true;
        SetHeaderCell(header.Cells[0], "#");
        SetHeaderCell(header.Cells[1], "Test case");
        SetHeaderCell(header.Cells[2], "Test details");
        SetHeaderCell(header.Cells[3], "Test date");
        SetHeaderCell(header.Cells[4], "Comment");
        SetHeaderCell(header.Cells[5], "Status");

        var row = table.AddRow();
        row.TopPadding = Unit.FromPoint(3);
        row.BottomPadding = Unit.FromPoint(3);
        row.Cells[0].AddParagraph(testCase.SortOrder.ToString());
        row.Cells[1].AddParagraph(ReportCellText(testCase.Title));
        row.Cells[2].AddParagraph(ReportCellText(testCase.ExpectedResult));
        row.Cells[3].AddParagraph(ReportCellText(testCase.LastStatusChangedDateDisplay));
        row.Cells[4].AddParagraph(ReportCellText(testCase.Comment));
        row.Cells[5].AddParagraph(StatusText(testCase.Status));
        SetStatusValueCell(row.Cells[5], testCase.Status);
        AddNestedList(row.Cells[1], "Meta", [$"{testCase.Checks.Count} checks", $"{testCase.Attachments.Count} attachments"]);

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

        var table = CreateTable(section, 1.1, 7.5, 7.5, 3.0, 3.8, 2.6);
        var header = table.AddRow();
        header.HeadingFormat = true;
        SetHeaderCell(header.Cells[0], "#");
        SetHeaderCell(header.Cells[1], "Check");
        SetHeaderCell(header.Cells[2], "Test details");
        SetHeaderCell(header.Cells[3], "Test date");
        SetHeaderCell(header.Cells[4], "Comment");
        SetHeaderCell(header.Cells[5], "Status");

        foreach (var check in checks)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromPoint(3);
            row.BottomPadding = Unit.FromPoint(3);
            row.Cells[0].AddParagraph(check.SortOrder.ToString());
            row.Cells[1].AddParagraph(ReportCellText(check.Text));
            row.Cells[2].AddParagraph(ReportCellText(check.ExpectedResult));
            row.Cells[3].AddParagraph(ReportCellText(check.LastStatusChangedDateDisplay));
            row.Cells[4].AddParagraph(ReportCellText(check.Comment));
            row.Cells[5].AddParagraph(StatusText(check.Status));
            SetStatusValueCell(row.Cells[5], check.Status);

            AddNestedList(
                row.Cells[1],
                "Fields",
                check.CustomFields
                    .Take(MaxCustomFieldsPerReportItem)
                    .Select(field => $"{ReportCellText(field.Name)}: {ReportCellText(field.DisplayValue)}"));
        }

        AddCheckImageAttachments(section, checks);
    }

    private static void AddCustomFields(Section section, IReadOnlyList<ReportCustomFieldItem> customFields)
    {
        if (customFields.Count == 0)
        {
            return;
        }

        var table = CreateTable(section, 7.0, 17.0);
        table.Format.SpaceBefore = Unit.FromPoint(5);
        table.Format.SpaceAfter = Unit.FromPoint(8);

        foreach (var field in customFields.Take(MaxCustomFieldsPerReportItem))
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromPoint(3);
            row.BottomPadding = Unit.FromPoint(3);
            SetLabelCell(row.Cells[0], field.Name);
            row.Cells[1].AddParagraph(ReportCellText(field.DisplayValue));
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
            row.Cells[0].AddParagraph(ReportCellText(attachment.OriginalFileName));
            row.Cells[1].AddParagraph(ReportCellText(attachment.ContentType));
            row.Cells[2].AddParagraph(ReportCellText(attachment.UploadedBy));
            row.Cells[3].AddParagraph(ReportCellText(attachment.UploadedDateDisplay));
        }

        AddImageAttachments(section, attachments);
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
            row.Cells[0].AddParagraph(ReportCellText(bug.Title));
            row.Cells[1].AddParagraph(ReportCellText(bug.Status));
            SetStatusValueCell(row.Cells[1], bug.Status);
            row.Cells[2].AddParagraph(ReportCellText(bug.Severity));
            row.Cells[3].AddParagraph(ReportCellText(bug.Priority));
            row.Cells[4].AddParagraph(ReportCellText(bug.LinkedEntityDisplayName));
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

    private static Row AddKeyValueRow(Table table, string firstKey, string firstValue, string secondKey, string secondValue)
    {
        var row = table.AddRow();
        SetLabelCell(row.Cells[0], firstKey);
        row.Cells[1].AddParagraph(ReportCellText(firstValue));
        SetLabelCell(row.Cells[2], secondKey);
        row.Cells[3].AddParagraph(ReportCellText(secondValue));
        return row;
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
        cell.AddParagraph(ReportCellText(text));
    }

    private static void SetStatusValueCell(Cell cell, object status)
    {
        cell.Shading.Color = StatusBackgroundColor(status);
        cell.Format.Font.Bold = true;
        cell.Format.Font.Color = StatusForegroundColor(status);
    }

    private static Color StatusBackgroundColor(object status)
    {
        return status switch
        {
            TestResultStatus.Pass => Color.FromRgb(220, 252, 231),
            TestResultStatus.Fail => Color.FromRgb(254, 226, 226),
            TestResultStatus.Blocked => Color.FromRgb(254, 243, 199),
            TestResultStatus.NotApplicable => Color.FromRgb(226, 232, 240),
            TestResultStatus.NotTested => Color.FromRgb(241, 245, 249),
            string value when value.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Fixed", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(220, 252, 231),
            string value when value.Equals("Open", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Reopened", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(254, 226, 226),
            string value when value.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                || value.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Ready For Retest", StringComparison.OrdinalIgnoreCase)
                || value.Equals("ReadyForRetest", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(219, 234, 254),
            string value when value.Equals("Rejected", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(226, 232, 240),
            _ => Color.FromRgb(241, 245, 249)
        };
    }

    private static Color StatusForegroundColor(object status)
    {
        return status switch
        {
            TestResultStatus.Pass => Color.FromRgb(22, 101, 52),
            TestResultStatus.Fail => Color.FromRgb(153, 27, 27),
            TestResultStatus.Blocked => Color.FromRgb(146, 64, 14),
            TestResultStatus.NotApplicable => Color.FromRgb(71, 85, 105),
            TestResultStatus.NotTested => Color.FromRgb(51, 65, 85),
            string value when value.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Fixed", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(22, 101, 52),
            string value when value.Equals("Open", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Reopened", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(153, 27, 27),
            string value when value.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                || value.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Ready For Retest", StringComparison.OrdinalIgnoreCase)
                || value.Equals("ReadyForRetest", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(30, 64, 175),
            string value when value.Equals("Rejected", StringComparison.OrdinalIgnoreCase) => Color.FromRgb(71, 85, 105),
            _ => Color.FromRgb(51, 65, 85)
        };
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
        heading.AddText(string.Join(", ", materializedItems.Select(ReportCellText)));
    }

    private static void AddImageAttachments(Section section, IReadOnlyList<ReportAttachmentItem> attachments)
    {
        var imageAttachments = attachments
            .Where(attachment => attachment.IsImage)
            .ToList();

        if (imageAttachments.Count == 0)
        {
            return;
        }

        var paragraph = section.AddParagraph("Image evidence");
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceBefore = Unit.FromPoint(5);

        AddImageAttachmentImages(section, imageAttachments);
    }

    private static void AddCheckImageAttachments(Section section, IReadOnlyList<ReportCheckItem> checks)
    {
        foreach (var check in checks)
        {
            var imageAttachments = check.Attachments
                .Where(attachment => attachment.IsImage)
                .ToList();

            if (imageAttachments.Count == 0)
            {
                continue;
            }

            var paragraph = section.AddParagraph(ReportCellText($"Image evidence for check {check.SortOrder}: {check.Text}"));
            paragraph.Format.Font.Bold = true;
            paragraph.Format.SpaceBefore = Unit.FromPoint(5);

            AddImageAttachmentImages(section, imageAttachments);
        }
    }

    private static void AddImageAttachmentImages(Section section, IReadOnlyList<ReportAttachmentItem> imageAttachments)
    {
        foreach (var attachment in imageAttachments)
        {
            var caption = section.AddParagraph(ReportCellText(attachment.OriginalFileName));
            caption.Format.Font.Size = 8;
            caption.Format.Font.Color = Colors.Gray;
            caption.Format.SpaceBefore = Unit.FromPoint(4);

            if (!File.Exists(attachment.AbsolutePath))
            {
                section.AddParagraph("Image file is missing and could not be embedded.");
                continue;
            }

            try
            {
                var image = section.AddImage(attachment.AbsolutePath);
                image.LockAspectRatio = true;
                image.Width = Unit.FromCentimeter(12);
            }
            catch (Exception)
            {
                section.AddParagraph("Image file could not be embedded safely.");
            }
        }
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

    private static string ReportCellText(string? value)
    {
        var text = EmptyAsDash(value);
        if (text == "-")
        {
            return text;
        }

        return string.Join(
            " ",
            text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(WrapLongToken));
    }

    private static string WrapLongToken(string token)
    {
        if (token.Length <= MaxReportTokenLength)
        {
            return token;
        }

        var chunks = new List<string>();
        for (var index = 0; index < token.Length; index += MaxReportTokenLength)
        {
            var length = Math.Min(MaxReportTokenLength, token.Length - index);
            chunks.Add(token.Substring(index, length));
        }

        return string.Join(Environment.NewLine, chunks);
    }
}
