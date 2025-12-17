using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Speech.Services;

/// <summary>
/// Preprocesses markdown text for TTS to produce natural-sounding speech.
/// Uses Markdig to parse markdown and converts it to speech-friendly plain text.
/// </summary>
public partial class TtsTextPreprocessor : ITtsTextPreprocessor
{
    private readonly ILogger<TtsTextPreprocessor> _logger;
    private readonly MarkdownPipeline _pipeline;

    /// <summary>
    /// Creates a new TtsTextPreprocessor.
    /// </summary>
    public TtsTextPreprocessor(ILogger<TtsTextPreprocessor> logger)
    {
        _logger = logger;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <inheritdoc />
    public string PreprocessForSpeech(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
            return string.Empty;

        try
        {
            var document = Markdown.Parse(markdownText, _pipeline);
            var result = new StringBuilder();

            ProcessBlock(document, result);

            var text = result.ToString();
            text = CleanupText(text);

            _logger.LogDebug("Preprocessed markdown for TTS: {OriginalLength} -> {ProcessedLength} chars",
                markdownText.Length, text.Length);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preprocess markdown, falling back to basic cleanup");
            return BasicMarkdownCleanup(markdownText);
        }
    }

    /// <summary>
    /// Processes a markdown block and its children recursively.
    /// </summary>
    private void ProcessBlock(MarkdownObject block, StringBuilder result)
    {
        switch (block)
        {
            case MarkdownDocument document:
                foreach (var child in document)
                    ProcessBlock(child, result);
                break;

            case HeadingBlock heading:
                ProcessInlines(heading.Inline, result);
                EnsureSentenceEnding(result);
                result.Append(' ');
                break;

            case ParagraphBlock paragraph:
                ProcessInlines(paragraph.Inline, result);
                EnsureSentenceEnding(result);
                result.Append(' ');
                break;

            case ListBlock list:
                ProcessList(list, result);
                break;

            case FencedCodeBlock:
            case CodeBlock:
                result.Append("I've included a code example. ");
                break;

            case QuoteBlock quote:
                foreach (var child in quote)
                    ProcessBlock(child, result);
                break;

            case ThematicBreakBlock:
                // Skip horizontal rules entirely
                break;

            case HtmlBlock:
                // Skip HTML blocks
                break;

            case Table table:
                ProcessTable(table, result);
                break;

            case LinkReferenceDefinitionGroup:
                // Skip link reference definitions
                break;

            default:
                // For any other block types, try to process children
                if (block is ContainerBlock container)
                {
                    foreach (var child in container)
                        ProcessBlock(child, result);
                }
                break;
        }
    }

    /// <summary>
    /// Processes a list block, converting to natural speech.
    /// </summary>
    private void ProcessList(ListBlock list, StringBuilder result)
    {
        var items = new List<string>();

        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                var itemText = new StringBuilder();
                foreach (var child in listItem)
                {
                    ProcessBlock(child, itemText);
                }
                var text = itemText.ToString().Trim();
                // Remove trailing periods for list items (we'll add proper punctuation)
                text = text.TrimEnd('.', ' ');
                if (!string.IsNullOrWhiteSpace(text))
                    items.Add(text);
            }
        }

        if (items.Count == 0)
            return;

        result.Append(FormatListForSpeech(items));
        result.Append(' ');
    }

    /// <summary>
    /// Formats a list of items for natural speech.
    /// </summary>
    private static string FormatListForSpeech(List<string> items)
    {
        return items.Count switch
        {
            0 => "",
            1 => items[0] + ".",
            2 => $"There are two items: {items[0]}, and {items[1]}.",
            3 => $"There are three items: {items[0]}, {items[1]}, and {items[2]}.",
            4 => $"There are four items: {items[0]}, {items[1]}, {items[2]}, and {items[3]}.",
            5 => $"There are five items: {items[0]}, {items[1]}, {items[2]}, {items[3]}, and {items[4]}.",
            _ => $"There are {items.Count} items: {string.Join(", ", items.Take(items.Count - 1))}, and {items.Last()}."
        };
    }

    /// <summary>
    /// Processes a table, converting to natural prose.
    /// Each row becomes a sentence combining column headers with cell values.
    /// </summary>
    private void ProcessTable(Table table, StringBuilder result)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0)
            return;

        // Extract headers from the first row (if it's a header row)
        var headers = new List<string>();
        var dataRows = new List<TableRow>();

        if (rows.Count > 0 && rows[0].IsHeader)
        {
            headers = ExtractRowCells(rows[0]);
            dataRows = rows.Skip(1).ToList();
        }
        else
        {
            // No header row - just process all rows as data
            dataRows = rows;
        }

        if (dataRows.Count == 0)
        {
            // Only headers, no data
            if (headers.Count > 0)
            {
                result.Append($"I've included a table with columns: {FormatListInline(headers)}. ");
            }
            return;
        }

        // Convert each data row to prose
        foreach (var row in dataRows)
        {
            var cells = ExtractRowCells(row);
            if (cells.Count == 0)
                continue;

            var rowText = FormatTableRowAsProse(headers, cells);
            if (!string.IsNullOrWhiteSpace(rowText))
            {
                result.Append(rowText);
                result.Append(' ');
            }
        }
    }

    /// <summary>
    /// Extracts cell text content from a table row.
    /// </summary>
    private List<string> ExtractRowCells(TableRow row)
    {
        var cells = new List<string>();
        foreach (var cell in row.OfType<TableCell>())
        {
            var cellText = new StringBuilder();
            foreach (var child in cell)
            {
                ProcessBlock(child, cellText);
            }
            var text = cellText.ToString().Trim().TrimEnd('.', ' ');
            cells.Add(text);
        }
        return cells;
    }

    /// <summary>
    /// Formats a table row as natural prose using column headers.
    /// Example: "Python is an interpreted language with dynamic typing."
    /// </summary>
    private static string FormatTableRowAsProse(List<string> headers, List<string> cells)
    {
        if (cells.Count == 0)
            return string.Empty;

        // If we have headers, try to create meaningful prose
        if (headers.Count > 0 && cells.Count > 0)
        {
            var parts = new List<string>();

            // First cell is typically the subject/name
            var subject = cells[0];
            if (string.IsNullOrWhiteSpace(subject))
                subject = "An item";

            // Build prose from remaining columns
            for (var i = 1; i < cells.Count && i < headers.Count; i++)
            {
                var header = headers[i].ToLowerInvariant();
                var value = cells[i];

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // Try to create natural phrasing based on common header patterns
                var phrase = CreatePhraseForColumn(header, value);
                if (!string.IsNullOrWhiteSpace(phrase))
                    parts.Add(phrase);
            }

            if (parts.Count == 0)
            {
                // No additional columns, just state the subject
                return $"{subject}.";
            }

            if (parts.Count == 1)
            {
                return $"{subject} {parts[0]}.";
            }

            // Join multiple attributes naturally
            var lastPart = parts[^1];
            var otherParts = string.Join(", ", parts.Take(parts.Count - 1));
            return $"{subject} {otherParts}, and {lastPart}.";
        }

        // No headers - just list the cell values
        if (cells.Count == 1)
        {
            return $"{cells[0]}.";
        }

        return $"{cells[0]}: {string.Join(", ", cells.Skip(1))}.";
    }

    /// <summary>
    /// Creates a natural phrase for a column header and value.
    /// </summary>
    private static string CreatePhraseForColumn(string header, string value)
    {
        // Common patterns for natural phrasing
        return header switch
        {
            // Pricing/cost patterns
            "price" or "cost" or "amount" => $"costs {value}",

            // Rating/score patterns
            "rating" or "score" or "stars" => $"has a rating of {value}",

            // Type/category patterns
            "type" or "category" or "kind" => $"is a {value}",

            // Description patterns
            "description" or "desc" or "details" => $"is described as {value}",

            // Status patterns
            "status" or "state" => $"has status {value}",

            // Date patterns
            "date" or "created" or "updated" => $"was {header} on {value}",

            // Count/quantity patterns
            "count" or "quantity" or "qty" or "number" => $"has {value}",

            // Boolean-like patterns
            "enabled" or "active" or "available" => value.ToLowerInvariant() is "yes" or "true" or "1"
                ? $"is {header}"
                : $"is not {header}",

            // Language/framework for programming comparisons
            "language" or "framework" or "platform" => $"uses {value}",

            // Performance patterns
            "speed" or "performance" => $"has {value} {header}",

            // Default: "has [header] [value]" or "is [value]" for short values
            _ => value.Length < 20 && !value.Contains(' ')
                ? $"has {header} {value}"
                : $"has {value} as its {header}"
        };
    }

    /// <summary>
    /// Formats a list inline without the "There are X items" prefix.
    /// </summary>
    private static string FormatListInline(List<string> items)
    {
        return items.Count switch
        {
            0 => "",
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => $"{string.Join(", ", items.Take(items.Count - 1))}, and {items.Last()}"
        };
    }

    /// <summary>
    /// Processes inline elements within a block.
    /// </summary>
    private void ProcessInlines(ContainerInline? container, StringBuilder result)
    {
        if (container == null)
            return;

        foreach (var inline in container)
        {
            ProcessInline(inline, result);
        }
    }

    /// <summary>
    /// Processes a single inline element.
    /// </summary>
    private void ProcessInline(Inline inline, StringBuilder result)
    {
        switch (inline)
        {
            case LiteralInline literal:
                result.Append(literal.Content);
                break;

            case EmphasisInline emphasis:
                // Strip emphasis markers, keep content
                ProcessInlines(emphasis, result);
                break;

            case CodeInline code:
                // Strip backticks, keep content
                result.Append(code.Content);
                break;

            case LinkInline link:
                // Keep link text, drop URL
                if (link.FirstChild != null)
                {
                    ProcessInlines(link, result);
                }
                else if (!string.IsNullOrEmpty(link.Title))
                {
                    result.Append(link.Title);
                }
                break;

            case AutolinkInline autolink:
                // For bare URLs, just say "a link"
                result.Append("a link");
                break;

            case LineBreakInline:
                result.Append(' ');
                break;

            case HtmlInline:
                // Skip inline HTML
                break;

            case HtmlEntityInline htmlEntity:
                // Convert HTML entities to their character equivalents
                result.Append(htmlEntity.Transcoded);
                break;

            case ContainerInline container:
                ProcessInlines(container, result);
                break;
        }
    }

    /// <summary>
    /// Ensures the text ends with sentence-ending punctuation.
    /// </summary>
    private static void EnsureSentenceEnding(StringBuilder result)
    {
        var text = result.ToString().TrimEnd();
        if (text.Length == 0)
            return;

        var lastChar = text[^1];
        if (lastChar != '.' && lastChar != '!' && lastChar != '?' && lastChar != ':')
        {
            result.Clear();
            result.Append(text);
            result.Append('.');
        }
    }

    /// <summary>
    /// Cleans up the final text for TTS.
    /// </summary>
    private string CleanupText(string text)
    {
        // Normalize whitespace
        text = MultipleSpacesRegex().Replace(text, " ");

        // Remove space before punctuation
        text = SpaceBeforePunctuationRegex().Replace(text, "$1");

        // Ensure space after punctuation (except at end)
        text = PunctuationWithoutSpaceRegex().Replace(text, "$1 ");

        // Clean up multiple periods
        text = MultiplePeriodsRegex().Replace(text, ".");

        // Trim
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Basic markdown cleanup fallback when parsing fails.
    /// </summary>
    private static string BasicMarkdownCleanup(string text)
    {
        // Remove code blocks
        text = Regex.Replace(text, @"```[\s\S]*?```", "I've included a code example.");
        text = Regex.Replace(text, @"`[^`]+`", match => match.Value.Trim('`'));

        // Remove emphasis markers
        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
        text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
        text = Regex.Replace(text, @"__([^_]+)__", "$1");
        text = Regex.Replace(text, @"_([^_]+)_", "$1");

        // Remove links, keep text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");

        // Remove headers
        text = Regex.Replace(text, @"^#{1,6}\s*", "", RegexOptions.Multiline);

        // Remove list markers
        text = Regex.Replace(text, @"^\s*[-*+]\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s*\d+\.\s+", "", RegexOptions.Multiline);

        // Remove horizontal rules
        text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "", RegexOptions.Multiline);

        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"\s+([.!?,;:])")]
    private static partial Regex SpaceBeforePunctuationRegex();

    [GeneratedRegex(@"([.!?])(?=[A-Za-z])")]
    private static partial Regex PunctuationWithoutSpaceRegex();

    [GeneratedRegex(@"\.{2,}")]
    private static partial Regex MultiplePeriodsRegex();
}
