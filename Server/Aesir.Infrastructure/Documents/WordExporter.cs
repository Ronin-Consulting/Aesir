using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Documents;

/// <summary>
/// Exports documents to Word format (.docx) using OpenXml.
/// </summary>
public class WordExporter : IDocumentExporter
{
    private readonly ILogger<WordExporter> _logger;

    // Color constants (hex without #, for OpenXml)
    private const string PrimaryColor = "1a365d";
    private const string SecondaryColor = "4a5568";
    private const string AccentColor = "3182ce";
    private const string BorderColor = "e2e8f0";

    public WordExporter(ILogger<WordExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public DocumentFormat Format => DocumentFormat.Word;

    /// <inheritdoc />
    public Task<ExportResult> ExportAsync(ExportDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Word export for document: {Title}", document.Title);

            using var memoryStream = new MemoryStream();

            using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
            {
                // Add main document part
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                // Add styles
                AddStyles(mainPart);

                var body = mainPart.Document.Body!;

                // Add title
                AddTitle(body, document);

                // Add metadata (author, date)
                if (!string.IsNullOrEmpty(document.Author) || document.CreatedAt != default)
                {
                    AddMetadata(body, document);
                }

                // Add horizontal rule after header
                AddHorizontalRule(body);

                // Add sections
                foreach (var section in document.Sections)
                {
                    if (section.PageBreakBefore)
                    {
                        AddPageBreak(body);
                    }

                    AddSection(body, section);
                }

                // Add footer text if specified
                if (!string.IsNullOrEmpty(document.FooterText))
                {
                    AddFooter(mainPart, document);
                }

                mainPart.Document.Save();
            }

            var wordBytes = memoryStream.ToArray();
            var fileName = SanitizeFileName(document.Title) + ".docx";

            _logger.LogInformation("Word export completed successfully. Size: {Size} bytes", wordBytes.Length);

            return Task.FromResult(ExportResult.Succeeded(
                wordBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Word document: {Title}", document.Title);
            return Task.FromResult(ExportResult.Failed($"Word export failed: {ex.Message}"));
        }
    }

    private void AddStyles(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();

        // Heading 1 style
        styles.Append(CreateHeadingStyle("Heading1", "Heading 1", "28", PrimaryColor, true));

        // Heading 2 style
        styles.Append(CreateHeadingStyle("Heading2", "Heading 2", "24", PrimaryColor, true));

        // Heading 3 style
        styles.Append(CreateHeadingStyle("Heading3", "Heading 3", "22", SecondaryColor, true));

        // Quote style
        styles.Append(CreateQuoteStyle());

        // Code style
        styles.Append(CreateCodeStyle());

        stylesPart.Styles = styles;
        stylesPart.Styles.Save();
    }

    private static Style CreateHeadingStyle(string styleId, string styleName, string fontSize, string color, bool bold)
    {
        return new Style(
            new StyleName { Val = styleName },
            new BasedOn { Val = "Normal" },
            new NextParagraphStyle { Val = "Normal" },
            new StyleRunProperties(
                new Bold { Val = OnOffValue.FromBoolean(bold) },
                new FontSize { Val = fontSize },
                new Color { Val = color }
            ),
            new StyleParagraphProperties(
                new SpacingBetweenLines { Before = "240", After = "120" }
            )
        )
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
            CustomStyle = true
        };
    }

    private static Style CreateQuoteStyle()
    {
        return new Style(
            new StyleName { Val = "Quote" },
            new BasedOn { Val = "Normal" },
            new StyleRunProperties(
                new Italic(),
                new Color { Val = SecondaryColor }
            ),
            new StyleParagraphProperties(
                new Indentation { Left = "720" },
                new ParagraphBorders(
                    new LeftBorder { Val = BorderValues.Single, Color = AccentColor, Size = 24 }
                ),
                new Shading { Val = ShadingPatternValues.Clear, Fill = "f7fafc" }
            )
        )
        {
            Type = StyleValues.Paragraph,
            StyleId = "Quote",
            CustomStyle = true
        };
    }

    private static Style CreateCodeStyle()
    {
        return new Style(
            new StyleName { Val = "Code" },
            new BasedOn { Val = "Normal" },
            new StyleRunProperties(
                new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" },
                new FontSize { Val = "18" },
                new Color { Val = "e2e8f0" }
            ),
            new StyleParagraphProperties(
                new Shading { Val = ShadingPatternValues.Clear, Fill = "2d3748" },
                new SpacingBetweenLines { Before = "120", After = "120" }
            )
        )
        {
            Type = StyleValues.Paragraph,
            StyleId = "Code",
            CustomStyle = true
        };
    }

    private void AddTitle(Body body, ExportDocument document)
    {
        var titleParagraph = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { After = "0" }
            ),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize { Val = "48" },
                    new Color { Val = PrimaryColor }
                ),
                new Text(document.Title)
            )
        );
        body.Append(titleParagraph);

        if (!string.IsNullOrEmpty(document.Subtitle))
        {
            var subtitleParagraph = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "60", After = "0" }
                ),
                new Run(
                    new RunProperties(
                        new FontSize { Val = "24" },
                        new Color { Val = SecondaryColor }
                    ),
                    new Text(document.Subtitle)
                )
            );
            body.Append(subtitleParagraph);
        }
    }

    private void AddMetadata(Body body, ExportDocument document)
    {
        var metaParagraph = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { Before = "120", After = "120" }
            )
        );

        if (!string.IsNullOrEmpty(document.Author))
        {
            metaParagraph.Append(new Run(
                new RunProperties(
                    new FontSize { Val = "18" },
                    new Color { Val = SecondaryColor }
                ),
                new Text(document.Author)
            ));

            metaParagraph.Append(new Run(
                new RunProperties(
                    new FontSize { Val = "18" },
                    new Color { Val = SecondaryColor }
                ),
                new Text(" | ") { Space = SpaceProcessingModeValues.Preserve }
            ));
        }

        metaParagraph.Append(new Run(
            new RunProperties(
                new FontSize { Val = "18" },
                new Color { Val = SecondaryColor }
            ),
            new Text(document.CreatedAt.ToString("MMMM d, yyyy"))
        ));

        body.Append(metaParagraph);
    }

    private static void AddHorizontalRule(Body body)
    {
        var hrParagraph = new Paragraph(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Color = BorderColor, Size = 6 }
                ),
                new SpacingBetweenLines { After = "240" }
            )
        );
        body.Append(hrParagraph);
    }

    private static void AddPageBreak(Body body)
    {
        body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
    }

    private void AddSection(Body body, DocumentSection section)
    {
        // Add section heading
        var headingStyleId = section.HeadingLevel switch
        {
            1 => "Heading1",
            2 => "Heading2",
            _ => "Heading3"
        };

        var headingParagraph = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = headingStyleId }
            ),
            new Run(new Text(section.Heading))
        );
        body.Append(headingParagraph);

        // Add section content
        foreach (var content in section.Content)
        {
            AddContent(body, content);
        }
    }

    private void AddContent(Body body, DocumentContent content)
    {
        switch (content)
        {
            case ParagraphContent paragraph:
                AddParagraphContent(body, paragraph);
                break;

            case MarkdownContent markdown:
                AddMarkdownContent(body, markdown);
                break;

            case ListContent list:
                AddListContent(body, list);
                break;

            case TableContent table:
                AddTableContent(body, table);
                break;

            case QuoteContent quote:
                AddQuoteContent(body, quote);
                break;

            case CodeContent code:
                AddCodeContent(body, code);
                break;
        }
    }

    private static void AddParagraphContent(Body body, ParagraphContent paragraph)
    {
        var runProperties = new RunProperties();

        if (paragraph.Bold)
            runProperties.Append(new Bold());

        if (paragraph.Italic)
            runProperties.Append(new Italic());

        var p = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { After = "120" }
            ),
            new Run(runProperties, new Text(paragraph.Text))
        );
        body.Append(p);
    }

    private void AddMarkdownContent(Body body, MarkdownContent markdown)
    {
        var lines = markdown.Markdown.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                body.Append(new Paragraph());
                continue;
            }

            // Handle basic markdown headers
            if (trimmedLine.StartsWith("### "))
            {
                AddHeading(body, trimmedLine[4..], "Heading3");
            }
            else if (trimmedLine.StartsWith("## "))
            {
                AddHeading(body, trimmedLine[3..], "Heading2");
            }
            else if (trimmedLine.StartsWith("# "))
            {
                AddHeading(body, trimmedLine[2..], "Heading1");
            }
            else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
            {
                AddBulletItem(body, trimmedLine[2..]);
            }
            else if (trimmedLine.StartsWith("> "))
            {
                AddQuoteContent(body, new QuoteContent { Text = trimmedLine[2..] });
            }
            else
            {
                var cleanedLine = RemoveMarkdownFormatting(trimmedLine);
                body.Append(new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { After = "120" }),
                    new Run(new Text(cleanedLine))
                ));
            }
        }
    }

    private static void AddHeading(Body body, string text, string styleId)
    {
        body.Append(new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(new Text(text))
        ));
    }

    private static void AddBulletItem(Body body, string text)
    {
        body.Append(new Paragraph(
            new ParagraphProperties(
                new Indentation { Left = "720" },
                new SpacingBetweenLines { After = "60" }
            ),
            new Run(new Text($"\u2022  {text}"))
        ));
    }

    private static void AddListContent(Body body, ListContent list)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            var item = list.Items[i];
            var bullet = list.Ordered ? $"{i + 1}." : "\u2022";

            body.Append(new Paragraph(
                new ParagraphProperties(
                    new Indentation { Left = "720" },
                    new SpacingBetweenLines { After = "60" }
                ),
                new Run(new Text($"{bullet}  {item}"))
            ));
        }
    }

    private void AddTableContent(Body body, TableContent tableContent)
    {
        var table = new Table();

        // Table properties
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Color = BorderColor, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Color = BorderColor, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Color = BorderColor, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Color = BorderColor, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Color = BorderColor, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Color = BorderColor, Size = 4 }
            ),
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" } // 100% width
        );
        table.Append(tableProperties);

        // Header row
        var headerRow = new TableRow();
        foreach (var header in tableContent.Headers)
        {
            var cell = new TableCell(
                new TableCellProperties(
                    new Shading { Val = ShadingPatternValues.Clear, Fill = PrimaryColor }
                ),
                new Paragraph(
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new Color { Val = "FFFFFF" }
                        ),
                        new Text(header)
                    )
                )
            );
            headerRow.Append(cell);
        }
        table.Append(headerRow);

        // Data rows
        var isAlternate = false;
        foreach (var row in tableContent.Rows)
        {
            var tableRow = new TableRow();
            var fillColor = isAlternate ? "f7fafc" : "FFFFFF";

            foreach (var cellValue in row)
            {
                var cell = new TableCell(
                    new TableCellProperties(
                        new Shading { Val = ShadingPatternValues.Clear, Fill = fillColor }
                    ),
                    new Paragraph(new Run(new Text(cellValue)))
                );
                tableRow.Append(cell);
            }

            table.Append(tableRow);
            isAlternate = !isAlternate;
        }

        body.Append(table);
        body.Append(new Paragraph()); // Add spacing after table
    }

    private static void AddQuoteContent(Body body, QuoteContent quote)
    {
        var quoteParagraph = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = "Quote" }
            ),
            new Run(
                new RunProperties(new Italic()),
                new Text($"\"{quote.Text}\"")
            )
        );
        body.Append(quoteParagraph);

        if (!string.IsNullOrEmpty(quote.Citation))
        {
            body.Append(new Paragraph(
                new ParagraphProperties(
                    new Indentation { Left = "720" },
                    new SpacingBetweenLines { After = "120" }
                ),
                new Run(
                    new RunProperties(
                        new FontSize { Val = "18" },
                        new Color { Val = SecondaryColor }
                    ),
                    new Text($"— {quote.Citation}")
                )
            ));
        }
    }

    private static void AddCodeContent(Body body, CodeContent code)
    {
        if (!string.IsNullOrEmpty(code.Language))
        {
            body.Append(new Paragraph(
                new Run(
                    new RunProperties(
                        new FontSize { Val = "16" },
                        new Color { Val = "a0aec0" }
                    ),
                    new Text(code.Language)
                )
            ));
        }

        var codeParagraph = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = "Code" }
            ),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" }
                ),
                new Text(code.Code)
            )
        );
        body.Append(codeParagraph);
    }

    private void AddFooter(MainDocumentPart mainPart, ExportDocument document)
    {
        var footerPart = mainPart.AddNewPart<FooterPart>();
        var footer = new Footer(
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }
                ),
                new Run(
                    new RunProperties(
                        new FontSize { Val = "16" },
                        new Color { Val = SecondaryColor }
                    ),
                    new Text(document.FooterText!)
                )
            )
        );
        footerPart.Footer = footer;
        footerPart.Footer.Save();

        // Link footer to section
        var sectionProperties = mainPart.Document.Body!.GetFirstChild<SectionProperties>();
        if (sectionProperties == null)
        {
            sectionProperties = new SectionProperties();
            mainPart.Document.Body.Append(sectionProperties);
        }

        sectionProperties.Append(new FooterReference
        {
            Type = HeaderFooterValues.Default,
            Id = mainPart.GetIdOfPart(footerPart)
        });
    }

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
