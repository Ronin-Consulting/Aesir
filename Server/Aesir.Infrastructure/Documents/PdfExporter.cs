using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aesir.Infrastructure.Documents;

/// <summary>
/// Exports documents to PDF format using QuestPDF.
/// </summary>
public class PdfExporter : IDocumentExporter
{
    private readonly ILogger<PdfExporter> _logger;

    // Color scheme for professional documents
    private static readonly string PrimaryColor = "#1a365d";  // Dark blue
    private static readonly string SecondaryColor = "#4a5568"; // Gray
    private static readonly string AccentColor = "#3182ce";   // Light blue
    private static readonly string BorderColor = "#e2e8f0";   // Light gray

    public PdfExporter(ILogger<PdfExporter> logger)
    {
        _logger = logger;

        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public DocumentFormat Format => DocumentFormat.Pdf;

    /// <inheritdoc />
    public Task<ExportResult> ExportAsync(ExportDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting PDF export for document: {Title}", document.Title);

            var pdfDocument = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    // Header
                    page.Header().Element(c => ComposeHeader(c, document));

                    // Content
                    page.Content().Element(c => ComposeContent(c, document));

                    // Footer
                    page.Footer().Element(c => ComposeFooter(c, document));
                });
            });

            var pdfBytes = pdfDocument.GeneratePdf();

            var fileName = SanitizeFileName(document.Title) + ".pdf";

            _logger.LogInformation("PDF export completed successfully. Size: {Size} bytes", pdfBytes.Length);

            return Task.FromResult(ExportResult.Succeeded(
                pdfBytes,
                "application/pdf",
                fileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export PDF for document: {Title}", document.Title);
            return Task.FromResult(ExportResult.Failed($"PDF export failed: {ex.Message}"));
        }
    }

    private void ComposeHeader(IContainer container, ExportDocument document)
    {
        container.Column(headerColumn =>
        {
            headerColumn.Item().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item()
                        .Text(document.Title)
                        .Bold()
                        .FontSize(18)
                        .FontColor(Color.FromHex(PrimaryColor));

                    if (!string.IsNullOrEmpty(document.Subtitle))
                    {
                        column.Item()
                            .Text(document.Subtitle)
                            .FontSize(12)
                            .FontColor(Color.FromHex(SecondaryColor));
                    }
                });

                row.ConstantItem(100).AlignRight().Column(column =>
                {
                    if (!string.IsNullOrEmpty(document.Author))
                    {
                        column.Item()
                            .Text(document.Author)
                            .FontSize(9)
                            .FontColor(Color.FromHex(SecondaryColor));
                    }

                    column.Item()
                        .Text(document.CreatedAt.ToString("MMMM d, yyyy"))
                        .FontSize(9)
                        .FontColor(Color.FromHex(SecondaryColor));
                });
            });

            headerColumn.Item().PaddingTop(10).BorderBottom(1).BorderColor(Color.FromHex(BorderColor));
        });
    }

    private void ComposeContent(IContainer container, ExportDocument document)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            foreach (var section in document.Sections)
            {
                if (section.PageBreakBefore)
                {
                    column.Item().PageBreak();
                }

                column.Item().Element(c => ComposeSection(c, section));
            }
        });
    }

    private void ComposeSection(IContainer container, DocumentSection section)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // Section heading
            var headingStyle = GetHeadingStyle(section.HeadingLevel);
            column.Item()
                .Text(text =>
                {
                    text.DefaultTextStyle(style =>
                    {
                        var s = style.FontSize(headingStyle.fontSize).FontColor(Color.FromHex(headingStyle.color));
                        if (headingStyle.bold) s = s.Bold();
                        return s;
                    });
                    text.Span(section.Heading);
                });

            // Section content
            foreach (var content in section.Content)
            {
                column.Item().Element(c => ComposeContent(c, content));
            }
        });
    }

    private void ComposeContent(IContainer container, DocumentContent content)
    {
        switch (content)
        {
            case ParagraphContent paragraph:
                ComposeParagraph(container, paragraph);
                break;

            case MarkdownContent markdown:
                ComposeMarkdown(container, markdown);
                break;

            case ListContent list:
                ComposeList(container, list);
                break;

            case TableContent table:
                ComposeTable(container, table);
                break;

            case QuoteContent quote:
                ComposeQuote(container, quote);
                break;

            case CodeContent code:
                ComposeCode(container, code);
                break;
        }
    }

    private void ComposeParagraph(IContainer container, ParagraphContent paragraph)
    {
        var text = container.Text(paragraph.Text);

        if (paragraph.Bold)
            text.Bold();

        if (paragraph.Italic)
            text.Italic();
    }

    private void ComposeMarkdown(IContainer container, MarkdownContent markdown)
    {
        // For markdown, we'll do basic rendering (convert to plain text with minimal formatting)
        // A full markdown parser could be integrated for more complex rendering
        var lines = markdown.Markdown.Split('\n');

        container.Column(column =>
        {
            column.Spacing(5);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    column.Item().Height(5);
                    continue;
                }

                // Handle basic markdown headers
                if (trimmedLine.StartsWith("### "))
                {
                    column.Item().Text(trimmedLine[4..]).Bold().FontSize(13);
                }
                else if (trimmedLine.StartsWith("## "))
                {
                    column.Item().Text(trimmedLine[3..]).Bold().FontSize(14);
                }
                else if (trimmedLine.StartsWith("# "))
                {
                    column.Item().Text(trimmedLine[2..]).Bold().FontSize(16);
                }
                else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(15).Text("\u2022").FontSize(11);
                        row.RelativeItem().Text(trimmedLine[2..]);
                    });
                }
                else if (trimmedLine.StartsWith("> "))
                {
                    column.Item()
                        .BorderLeft(3)
                        .BorderColor(Color.FromHex(AccentColor))
                        .PaddingLeft(10)
                        .Text(trimmedLine[2..])
                        .Italic()
                        .FontColor(Color.FromHex(SecondaryColor));
                }
                else
                {
                    // Remove basic markdown formatting for plain text
                    var cleanedLine = RemoveMarkdownFormatting(trimmedLine);
                    column.Item().Text(cleanedLine);
                }
            }
        });
    }

    private void ComposeList(IContainer container, ListContent list)
    {
        container.Column(column =>
        {
            column.Spacing(3);

            for (var i = 0; i < list.Items.Count; i++)
            {
                var item = list.Items[i];
                var bullet = list.Ordered ? $"{i + 1}." : "\u2022";

                column.Item().Row(row =>
                {
                    row.ConstantItem(20).Text(bullet).FontSize(11);
                    row.RelativeItem().Text(item);
                });
            }
        });
    }

    private void ComposeTable(IContainer container, TableContent table)
    {
        container.Table(t =>
        {
            // Define columns
            t.ColumnsDefinition(columns =>
            {
                foreach (var _ in table.Headers)
                {
                    columns.RelativeColumn();
                }
            });

            // Header row
            t.Header(header =>
            {
                foreach (var headerText in table.Headers)
                {
                    header.Cell()
                        .Background(Color.FromHex(PrimaryColor))
                        .Padding(8)
                        .Text(headerText)
                        .FontColor(Colors.White)
                        .Bold();
                }
            });

            // Data rows
            var isAlternate = false;
            foreach (var row in table.Rows)
            {
                var bgColor = isAlternate ? Color.FromHex("#f7fafc") : Colors.White;

                foreach (var cell in row)
                {
                    t.Cell()
                        .Background(bgColor)
                        .BorderBottom(1)
                        .BorderColor(Color.FromHex(BorderColor))
                        .Padding(6)
                        .Text(cell);
                }

                isAlternate = !isAlternate;
            }
        });
    }

    private void ComposeQuote(IContainer container, QuoteContent quote)
    {
        container
            .Background(Color.FromHex("#f7fafc"))
            .BorderLeft(4)
            .BorderColor(Color.FromHex(AccentColor))
            .Padding(12)
            .Column(column =>
            {
                column.Item()
                    .Text($"\"{quote.Text}\"")
                    .Italic()
                    .FontSize(11);

                if (!string.IsNullOrEmpty(quote.Citation))
                {
                    column.Item()
                        .PaddingTop(5)
                        .Text($"— {quote.Citation}")
                        .FontSize(9)
                        .FontColor(Color.FromHex(SecondaryColor));
                }
            });
    }

    private void ComposeCode(IContainer container, CodeContent code)
    {
        container
            .Background(Color.FromHex("#2d3748"))
            .Padding(12)
            .Column(column =>
            {
                if (!string.IsNullOrEmpty(code.Language))
                {
                    column.Item()
                        .PaddingBottom(5)
                        .Text(code.Language)
                        .FontSize(9)
                        .FontColor(Color.FromHex("#a0aec0"));
                }

                column.Item()
                    .Text(code.Code)
                    .FontFamily(Fonts.CourierNew)
                    .FontSize(9)
                    .FontColor(Color.FromHex("#e2e8f0"));
            });
    }

    private void ComposeFooter(IContainer container, ExportDocument document)
    {
        container.BorderTop(1).BorderColor(Color.FromHex(BorderColor)).PaddingTop(10).Row(row =>
        {
            if (!string.IsNullOrEmpty(document.FooterText))
            {
                row.RelativeItem()
                    .Text(document.FooterText)
                    .FontSize(8)
                    .FontColor(Color.FromHex(SecondaryColor));
            }
            else
            {
                row.RelativeItem();
            }

            if (document.IncludePageNumbers)
            {
                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Color.FromHex(SecondaryColor)));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            }
        });
    }

    private static (int fontSize, string color, bool bold) GetHeadingStyle(int level) => level switch
    {
        1 => (16, PrimaryColor, true),
        2 => (14, PrimaryColor, true),
        3 => (12, SecondaryColor, true),
        _ => (11, SecondaryColor, true)
    };

    private static string RemoveMarkdownFormatting(string text)
    {
        // Remove bold
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"__(.+?)__", "$1");

        // Remove italic
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"_(.+?)_", "$1");

        // Remove inline code
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "$1");

        // Remove links (keep link text)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[(.+?)\]\(.+?\)", "$1");

        return text;
    }

    private static string SanitizeFileName(string fileName)
    {
        // Combine platform-specific invalid chars with additional problematic characters
        var platformInvalid = Path.GetInvalidFileNameChars();
        var additionalInvalid = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        var invalid = platformInvalid.Union(additionalInvalid).ToArray();

        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }
}
