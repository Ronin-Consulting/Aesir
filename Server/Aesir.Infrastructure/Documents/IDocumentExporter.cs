namespace Aesir.Infrastructure.Documents;

/// <summary>
/// Defines the supported document export formats.
/// </summary>
public enum DocumentFormat
{
    /// <summary>
    /// PDF format.
    /// </summary>
    Pdf,

    /// <summary>
    /// Microsoft Word format (.docx).
    /// </summary>
    Word
}

/// <summary>
/// Represents a structured document for export.
/// </summary>
public class ExportDocument
{
    /// <summary>
    /// Document title displayed on the cover/header.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional subtitle.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Author or organization name.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Date of the document.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The sections of the document.
    /// </summary>
    public List<DocumentSection> Sections { get; set; } = [];

    /// <summary>
    /// Optional footer text (appears on every page).
    /// </summary>
    public string? FooterText { get; set; }

    /// <summary>
    /// Whether to include page numbers.
    /// </summary>
    public bool IncludePageNumbers { get; set; } = true;

    /// <summary>
    /// Optional document metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Represents a section in the document.
/// </summary>
public class DocumentSection
{
    /// <summary>
    /// Section heading (e.g., "Executive Summary", "Findings").
    /// </summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>
    /// Heading level (1 = H1, 2 = H2, etc.).
    /// </summary>
    public int HeadingLevel { get; set; } = 1;

    /// <summary>
    /// The content blocks within this section.
    /// </summary>
    public List<DocumentContent> Content { get; set; } = [];

    /// <summary>
    /// Whether to start this section on a new page.
    /// </summary>
    public bool PageBreakBefore { get; set; }
}

/// <summary>
/// Base class for document content blocks.
/// </summary>
public abstract class DocumentContent
{
    /// <summary>
    /// The type of content.
    /// </summary>
    public abstract DocumentContentType ContentType { get; }
}

/// <summary>
/// Types of content that can appear in a document.
/// </summary>
public enum DocumentContentType
{
    /// <summary>
    /// Plain text paragraph.
    /// </summary>
    Paragraph,

    /// <summary>
    /// Markdown content (will be converted to appropriate format).
    /// </summary>
    Markdown,

    /// <summary>
    /// Bullet or numbered list.
    /// </summary>
    List,

    /// <summary>
    /// Data table.
    /// </summary>
    Table,

    /// <summary>
    /// Block quote or highlighted text.
    /// </summary>
    Quote,

    /// <summary>
    /// Code block.
    /// </summary>
    Code
}

/// <summary>
/// A text paragraph content block.
/// </summary>
public class ParagraphContent : DocumentContent
{
    public override DocumentContentType ContentType => DocumentContentType.Paragraph;

    /// <summary>
    /// The paragraph text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether the text is bold.
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// Whether the text is italic.
    /// </summary>
    public bool Italic { get; set; }
}

/// <summary>
/// Markdown content that will be rendered appropriately.
/// </summary>
public class MarkdownContent : DocumentContent
{
    public override DocumentContentType ContentType => DocumentContentType.Markdown;

    /// <summary>
    /// The markdown text.
    /// </summary>
    public string Markdown { get; set; } = string.Empty;
}

/// <summary>
/// A list content block.
/// </summary>
public class ListContent : DocumentContent
{
    public override DocumentContentType ContentType => DocumentContentType.List;

    /// <summary>
    /// The list items.
    /// </summary>
    public List<string> Items { get; set; } = [];

    /// <summary>
    /// Whether this is an ordered (numbered) list.
    /// </summary>
    public bool Ordered { get; set; }
}

/// <summary>
/// A table content block.
/// </summary>
public class TableContent : DocumentContent
{
    public override DocumentContentType ContentType => DocumentContentType.Table;

    /// <summary>
    /// Table headers.
    /// </summary>
    public List<string> Headers { get; set; } = [];

    /// <summary>
    /// Table rows (each row is a list of cell values).
    /// </summary>
    public List<List<string>> Rows { get; set; } = [];
}

/// <summary>
/// A block quote content block.
/// </summary>
public class QuoteContent : DocumentContent
{
    public override DocumentContentType ContentType => DocumentContentType.Quote;

    /// <summary>
    /// The quoted text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional citation/source.
    /// </summary>
    public string? Citation { get; set; }
}

/// <summary>
/// A code block content block.
/// </summary>
public class CodeContent : DocumentContent
{
    public override DocumentContentType ContentType => DocumentContentType.Code;

    /// <summary>
    /// The code text.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Optional language for syntax highlighting hints.
    /// </summary>
    public string? Language { get; set; }
}

/// <summary>
/// Result of a document export operation.
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Whether the export was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The exported document bytes.
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// The MIME type of the exported document.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Suggested filename for the exported document.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Error message if export failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ExportResult Succeeded(byte[] data, string contentType, string fileName) => new()
    {
        Success = true,
        Data = data,
        ContentType = contentType,
        FileName = fileName
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ExportResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Interface for document exporters.
/// </summary>
public interface IDocumentExporter
{
    /// <summary>
    /// The format this exporter produces.
    /// </summary>
    DocumentFormat Format { get; }

    /// <summary>
    /// Exports the document to the target format.
    /// </summary>
    /// <param name="document">The document to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The export result containing the document bytes.</returns>
    Task<ExportResult> ExportAsync(ExportDocument document, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating document exporters.
/// </summary>
public interface IDocumentExporterFactory
{
    /// <summary>
    /// Gets an exporter for the specified format.
    /// </summary>
    /// <param name="format">The document format.</param>
    /// <returns>The appropriate exporter.</returns>
    IDocumentExporter GetExporter(DocumentFormat format);

    /// <summary>
    /// Gets all available exporters.
    /// </summary>
    IEnumerable<IDocumentExporter> GetAllExporters();
}
