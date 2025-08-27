using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Css.Dom;
using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Aesir.Common.FileTypes;
using AngleSharp.Css;
using AngleSharp.Css.Parser;
using AngleSharp.Text;
using ColorCode.Styling;
using CsvHelper;
using Markdig;
using Markdown.ColorCode;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Converts the content of a text file into a PNG image.
/// </summary>
public class TextFileToPngConverter
{
    /// <summary>
    /// Converts the given text-based content into a PNG image by converting it to HTML first
    /// and then rendering it as an image.
    /// </summary>
    /// <param name="content">The content of the text file to be converted.</param>
    /// <param name="mimeType">The MIME type of the content, used to determine the parsing method.</param>
    /// <returns>Returns a <see cref="Bitmap"/> object representing the rendered PNG image, or null if the conversion fails.</returns>
    public async Task<Bitmap?> ConvertToPngAsync(string content, string mimeType)
    {
        // Parse to HTML
        var parser = new FileParser();
        var htmlContent = parser.ToHtml(content, mimeType);

        // Render HTML to PNG
        var renderer = new HtmlToPngRenderer();
        return await renderer.RenderHtmlToPngAsync(htmlContent).ConfigureAwait(false);
    }
}

/// <summary>
/// Provides functionality to parse content of various MIME types into HTML format.
/// </summary>
public class FileParser
{
    /// <summary>
    /// Converts the specified string content into an HTML representation based on the provided MIME type.
    /// </summary>
    /// <param name="content">The textual content to be converted to HTML.</param>
    /// <param name="mimeType">The MIME type of the input content, which determines the conversion method.</param>
    /// <returns>A string containing the HTML representation of the provided content.</returns>
    public string ToHtml(string content, string mimeType)
    {
        return mimeType switch
        {
            FileTypeManager.MimeTypes.Markdown => MarkdownToHtml(content),
            FileTypeManager.MimeTypes.Csv => CsvToHtml(content),
            FileTypeManager.MimeTypes.Xml => XmlToHtml(content),
            FileTypeManager.MimeTypes.Json => JsonToHtml(content),
            FileTypeManager.MimeTypes.PlainText => PlainTextToHtml(content),
            FileTypeManager.MimeTypes.Html => HtmlToHtml(content),
            _ => throw new ArgumentException($"Unsupported mime type: {mimeType}")
        };
    }

    /// <summary>
    /// Converts a Markdown-formatted string to its HTML representation.
    /// </summary>
    /// <param name="markdownText">The input string containing Markdown-formatted text.</param>
    /// <returns>A string containing the HTML representation of the input Markdown text.</returns>
    private string MarkdownToHtml(string markdownText)
    {
        var pipeline = GetMarkdownPipeline();

        var writer = new StringWriter(new StringBuilder(100000));
        var renderer = new Markdig.Renderers.HtmlRenderer(writer);

        pipeline.Setup(renderer);

        var doc = Markdig.Markdown.Parse(markdownText, pipeline);
        renderer.Render(doc);
        writer.Flush();

        return writer.ToString();
    }

    /// Converts a CSV-formatted string into an HTML table representation.
    /// <param name="csvText">The CSV-formatted string to be converted into an HTML table.</param>
    /// <returns>A string containing the HTML representation of the CSV data as a table.</returns>
    private string CsvToHtml(string csvText)
    {
        using var reader = new StringReader(csvText);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>().ToList();
        var headers = csv.HeaderRecord ?? [];

        var sb = new StringBuilder();
        sb.Append("<table border='1' style='border-collapse: collapse; font-family: Arial;'>");
        sb.Append("<tr>");
        foreach (var header in headers)
        {
            sb.Append($"<th style='padding: 5px;'>{System.Web.HttpUtility.HtmlEncode(header)}</th>");
        }

        sb.Append("</tr>");

        foreach (var record in records)
        {
            sb.Append("<tr>");
            foreach (var header in headers)
            {
                var value = ((IDictionary<string, object>)record)[header];
                sb.Append($"<td style='padding: 5px;'>{System.Web.HttpUtility.HtmlEncode(value?.ToString())}</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Converts an XML string into an HTML representation.
    /// </summary>
    /// <param name="xmlText">The XML string to be converted.</param>
    /// <returns>A string containing the HTML representation of the provided XML.</returns>
    private string XmlToHtml(string xmlText)
    {
        var doc = XDocument.Parse(xmlText);
        var sb = new StringBuilder();
        sb.Append("<pre style='font-family: Consolas; font-size: 14px;'>");
        sb.Append(System.Web.HttpUtility.HtmlEncode(doc.ToString()));
        sb.Append("</pre>");
        return sb.ToString();
    }
    
    private string HtmlToHtml(string htmlText)
    {
        var sb = new StringBuilder();
        sb.Append("<pre style='font-family: Consolas; font-size: 14px;'>");
        sb.Append(System.Web.HttpUtility.HtmlEncode(htmlText));
        sb.Append("</pre>");
        return sb.ToString();
    }

    /// <summary>
    /// Converts a JSON string into a formatted HTML representation.
    /// It wraps the JSON content in a styled <pre> tag with proper escaping to ensure it is web-safe.
    /// </summary>
    /// <param name="jsonText">The JSON string to be converted to HTML. Must be a valid JSON structure.</param>
    /// <returns>A string containing the HTML representation of the JSON.</returns>
    private string JsonToHtml(string jsonText)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(jsonText);
        var formattedJson = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
        var sb = new StringBuilder();
        sb.Append("<pre style='font-family: Consolas; font-size: 14px;'>");
        sb.Append(System.Web.HttpUtility.HtmlEncode(formattedJson));
        sb.Append("</pre>");
        return sb.ToString();
    }

    /// <summary>
    /// Converts plain text content into an HTML-formatted string with proper escaping.
    /// </summary>
    /// <param name="text">The plain text content to be converted to HTML.</param>
    /// <returns>An HTML string representation of the provided plain text.</returns>
    private string PlainTextToHtml(string text)
    {
        return $"<pre style='font-family: Consolas; font-size: 14px;'>{System.Web.HttpUtility.HtmlEncode(text)}</pre>";
    }

    /// Creates and configures a MarkdownPipeline object with optional settings for ColorCode support.
    /// <param name="useColorCode">Determines whether syntax highlighting with ColorCode should be enabled. Defaults to true.</param>
    /// <returns>A configured instance of MarkdownPipeline.</returns>
    private static MarkdownPipeline GetMarkdownPipeline(bool useColorCode = true)
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley();

        if (useColorCode)
            builder.UseColorCode(styleDictionary: StyleDictionary.DefaultDark);

        return builder.Build();
    }
}

/// <summary>
/// Provides functionality to render HTML content as a PNG image.
/// </summary>
/// <remarks>
/// This class utilizes SkiaSharp for image rendering and Avalonia for producing bitmap output.
/// It parses HTML content with CSS support via AngleSharp and dynamically measures and lays
/// out the content on a canvas.
/// </remarks>
public class HtmlToPngRenderer
{
    /// Renders an HTML content string to a PNG image asynchronously. Allows an optional output file path to save the PNG directly to disk.
    /// <param name="htmlContent">The HTML content to be rendered to PNG.</param>
    /// <param name="outputPath">Optional parameter specifying the full file path to save the rendered PNG. If null, the PNG is only created and returned as a Bitmap.</param>
    /// <returns>A Task representing the asynchronous operation, returning a Bitmap object if successful, or null in case of an error.</returns>
    public async Task<Bitmap?> RenderHtmlToPngAsync(string htmlContent, string? outputPath = null)
    {
        try
        {
            // A4 letter at 300DPI == 2480 × 3508
            // Step 1: Parse HTML with CSS support using BrowsingContext for computed styles
            var config = Configuration.Default.WithCss().WithRenderDevice(new DefaultRenderDevice
            {
                DeviceHeight = 3508,
                DeviceWidth = 2480,
            });
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req =>
                req.Content(htmlContent)).ConfigureAwait(false);

            // Step 2: Measure dimensions dynamically
            var (maxWidth, totalHeight) = MeasureBody(document.Body);
            var canvasWidth = Math.Max(800, maxWidth + 20); // Min width + padding
            var canvasHeight = Math.Max(600, totalHeight + 40); // Min height + padding

            using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Step 3: Render the body content
            float yOffset = 20;
            _ = RenderElement(document.Body, canvas, 10, yOffset, canvasWidth);

            // Step 4: Snapshot and encode to PNG data
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            // Optional: Save to file if path provided
            if (!string.IsNullOrEmpty(outputPath))
            {
                using var stream = File.OpenWrite(outputPath);
                data.SaveTo(stream);
            }

            // Step 5: Create and return Avalonia Bitmap
            using var memoryStream = new MemoryStream();
            data.SaveTo(memoryStream);
            memoryStream.Position = 0;
            return new Bitmap(memoryStream);
        }
        catch (Exception ex)
        {
            // Log error (integrate with AESIR's logging, e.g., via Serilog or Semantic Kernel's telemetry)
            Console.WriteLine($"Error rendering HTML to PNG: {ex.Message}");
            return null;
        }
    }

    /// Renders an HTML element and its children to the specified SkiaSharp canvas.
    /// The method processes and draws each child element recursively, including handling
    /// text, paragraphs, preformatted text, and tables.
    /// <param name="element">The root HTML element to render.</param>
    /// <param name="canvas">The SkiaSharp canvas to which the element will be rendered.</param>
    /// <param name="x">The X-coordinate (in pixels) where the rendering should start.</param>
    /// <param name="y">The Y-coordinate (in pixels) where the rendering should start.</param>
    /// <param name="containerWidth">The width of the container in which the element is rendered, for layout purposes.</param>
    /// <returns>
    /// The updated Y-coordinate (in pixels) after rendering all the content of the element,
    /// indicating the vertical position for the next rendering operation.
    /// </returns>
    private float RenderElement(IElement element, SKCanvas canvas, float x, float y, float containerWidth)
    {
        var currentY = y;

        foreach (var child in element.Children)
        {
            var style = child.ComputeCurrentStyle();
            using var paint = CreatePaintFromStyle(style, SKColors.Black, 16, "Arial", SKFontStyle.Normal);

            if (child is IText textNode)
            {
                var widthString = new StringSource(child.ComputeCurrentStyle().GetWidth());
                var clientWidth = widthString.ParseLength();

                var effectiveWidth = clientWidth.AsInt32() > 0 ? clientWidth.AsInt32() : containerWidth - 20;
                var align = style?.GetPropertyValue("text-align") ?? "left";
                DrawTextWithAlign(canvas, textNode.Text, x, currentY, paint, align, effectiveWidth);
                currentY += paint.TextSize + 4; // Line height
            }
            else if (child.TagName == "P" || child.TagName == "DIV")
            {
                // Render paragraph or div (recursive)
                currentY = RenderElement(child, canvas, x, currentY, containerWidth);
                currentY += 10; // Paragraph spacing
            }
            else if (child.TagName == "PRE")
            {
                // Handle <pre>
                currentY = RenderPre(child, canvas, x, currentY, containerWidth);
            }
            else if (child.TagName == "TABLE")
            {
                // Handle <table>
                currentY = RenderTable(child, canvas, x, currentY);
            }
        }

        return currentY;
    }

    /// <summary>
    /// Renders the content of a <pre> HTML element onto a SkiaSharp canvas.
    /// This method processes the text content of the element line by line,
    /// applies the computed styles, and aligns the text based on the element's CSS properties.
    /// </summary>
    /// <param name="preElement">The <see cref="IElement"/> corresponding to the <pre> HTML element to be rendered.</param>
    /// <param name="canvas">The <see cref="SKCanvas"/> on which the <pre> content is drawn.</param>
    /// <param name="x">The x-coordinate position on the canvas where the rendering begins.</param>
    /// <param name="y">The y-coordinate position on the canvas where the rendering begins.</param>
    /// <param name="containerWidth">The width of the container available for rendering the content.</param>
    /// <returns>The updated y-coordinate after the element has been rendered, including spacing adjustments.</returns>
    private float RenderPre(IElement preElement, SKCanvas canvas, float x, float y, float containerWidth)
    {
        var style = preElement.ComputeCurrentStyle();
        using var prePaint = CreatePaintFromStyle(style, SKColors.Black, 14, "Courier New", SKFontStyle.Normal);
        var align = style?.GetPropertyValue("text-align") ?? "left";

        var widthString = new StringSource(style.GetWidth());
        var clientWidth = widthString.ParseLength();

        var lines = preElement.TextContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentY = y;
        foreach (var line in lines)
        {
            var effectiveWidth = clientWidth.AsInt32() > 0 ? clientWidth.AsInt32() : containerWidth - 20;
            DrawTextWithAlign(canvas, line, x, currentY, prePaint, align, effectiveWidth);
            currentY += prePaint.TextSize + 4;
        }

        return currentY + 10; // Block spacing
    }

    /// <summary>
    /// Renders an HTML <table> element onto the specified SKCanvas.
    /// This method measures the table structure, computes cell dimensions, and draws the table content with borders and aligned text.
    /// </summary>
    /// <param name="tableElement">The HTML table element to be rendered.</param>
    /// <param name="canvas">The canvas on which the table will be drawn.</param>
    /// <param name="x">The X-coordinate for the position of the table on the canvas.</param>
    /// <param name="y">The Y-coordinate for the position of the table on the canvas.</param>
    /// <returns>The updated Y-coordinate after rendering the table, accounting for the table's height and spacing below it.</returns>
    private float RenderTable(IElement tableElement, SKCanvas canvas, float x, float y)
    {
        var rows = tableElement.QuerySelectorAll("tr").ToList();
        if (!rows.Any()) return y;

        var colCount = rows.Max(r => r.Children.Length);
        var colWidths = new float[colCount];
        var rowHeights = new float[rows.Count];

        // Pass 1: Measure
        using var measurePaint = new SKPaint { TextSize = 16 }; // Generic for measurement
        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var cells = rows[rowIdx].Children.Where(c => c.TagName == "TD" || c.TagName == "TH").ToList();
            float rowHeight = 20;
            for (var colIdx = 0; colIdx < cells.Count; colIdx++)
            {
                var cellText = cells[colIdx].TextContent.Trim();
                var textBounds = new SKRect();
                measurePaint.MeasureText(cellText, ref textBounds);
                colWidths[colIdx] = Math.Max(colWidths[colIdx], textBounds.Width + 20);
                rowHeight = Math.Max(rowHeight, textBounds.Height + 20);
            }

            rowHeights[rowIdx] = rowHeight;
        }

        // Pass 2: Draw
        using var borderPaint = new SKPaint { Color = SKColors.Gray, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        var currentY = y;
        var tableWidth = colWidths.Sum() + colCount;

        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var currentX = x;
            var row = rows[rowIdx];
            var rowStyle = row.ComputeCurrentStyle();
            var cells = row.Children.Where(c => c.TagName == "TD" || c.TagName == "TH").ToList();
            var isHeader = cells.Any(c => c.TagName == "TH");

            for (var colIdx = 0; colIdx < cells.Count; colIdx++)
            {
                var cell = cells[colIdx];
                var cellStyle = cell.ComputeCurrentStyle();
                using var cellPaint = CreatePaintFromStyle(cellStyle, SKColors.Black, 16, "Arial",
                    isHeader ? SKFontStyle.Bold : SKFontStyle.Normal);
                var cellText = cell.TextContent.Trim();
                var align = cellStyle?.GetPropertyValue("text-align") ?? "left";

                // Draw border
                canvas.DrawRect(currentX, currentY, colWidths[colIdx], rowHeights[rowIdx], borderPaint);

                // Draw text with alignment
                DrawTextWithAlign(canvas, cellText, currentX + 10,
                    currentY + (rowHeights[rowIdx] / 2) + (cellPaint.TextSize / 2) - 4, cellPaint, align,
                    colWidths[colIdx] - 20);

                currentX += colWidths[colIdx];
            }

            // Horizontal line
            canvas.DrawLine(x, currentY + rowHeights[rowIdx], x + tableWidth, currentY + rowHeights[rowIdx],
                borderPaint);

            currentY += rowHeights[rowIdx];
        }

        return currentY + 20; // Table spacing
    }

    /// <summary>
    /// Creates an <see cref="SKPaint"/> object based on the provided CSS style declarations and fallback values.
    /// </summary>
    /// <param name="style">The CSS style declaration to derive paint properties from. If null, default values are used.</param>
    /// <param name="defaultColor">The default color to use if no color is specified in the CSS style.</param>
    /// <param name="defaultSize">The default font size to use if no size is specified in the CSS style.</param>
    /// <param name="defaultFamily">The default font family to use if no font family is specified in the CSS style.</param>
    /// <param name="defaultStyle">The default font style to use if no style is specified in the CSS style.</param>
    /// <returns>A configured <see cref="SKPaint"/> object based on the provided CSS style or fallback values.</returns>
    private SKPaint CreatePaintFromStyle(ICssStyleDeclaration? style, SKColor defaultColor, float defaultSize,
        string defaultFamily, SKFontStyle defaultStyle)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = defaultSize,
            Typeface = SKTypeface.FromFamilyName(defaultFamily, defaultStyle)
        };

        // Color
        var colorStr = style?.GetPropertyValue("color");
        if (!string.IsNullOrEmpty(colorStr) && SKColor.TryParse(colorStr, out var color))
        {
            paint.Color = color;
        }
        else
        {
            paint.Color = defaultColor;
        }

        // Font weight
        var weightStr = style?.GetPropertyValue("font-weight");
        if (!string.IsNullOrEmpty(weightStr) &&
            (weightStr == "bold" || (int.TryParse(weightStr, out var weight) && weight >= 700)))
        {
            paint.Typeface = SKTypeface.FromFamilyName(defaultFamily, SKFontStyle.Bold);
        }

        // Font style
        var fontStyleStr = style?.GetPropertyValue("font-style");
        if (fontStyleStr == "italic")
        {
            paint.Typeface = SKTypeface.FromFamilyName(defaultFamily, SKFontStyle.Italic);
        }

        return paint;
    }

    /// <summary>
    /// Draws a text on the canvas with horizontal alignment within the specified container width.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas where the text will be drawn.</param>
    /// <param name="text">The text string to be drawn.</param>
    /// <param name="x">The x-coordinate of the initial position to draw the text.</param>
    /// <param name="y">The y-coordinate of the position to draw the text.</param>
    /// <param name="paint">The SkiaSharp paint object providing attributes like color, font, and size for rendering the text.</param>
    /// <param name="align">The text alignment mode, such as "left", "center", or "right".</param>
    /// <param name="containerWidth">The width of the container within which the text is aligned.</param>
    private void DrawTextWithAlign(SKCanvas canvas, string text, float x, float y, SKPaint paint, string align,
        float containerWidth)
    {
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);
        var drawX = x;

        if (align == "center")
        {
            drawX += (containerWidth - textBounds.Width) / 2;
        }
        else if (align == "right")
        {
            drawX += containerWidth - textBounds.Width;
        }

        canvas.DrawText(text, drawX, y, paint);
    }

    /// <summary>
    /// Measures the dimensions of the body content within an HTML document.
    /// This method calculates the maximum width and total height of the body element,
    /// based on its children and their respective dimensions.
    /// </summary>
    /// <param name="body">The <see cref="IElement"/> representing the body of the HTML document to measure.</param>
    /// <returns>A tuple containing the maximum width and total height
    /// of the measured body content, expressed as integers.</returns>
    private (int maxWidth, int totalHeight) MeasureBody(IElement body)
    {
        var maxW = 0;
        var totalH = 0;
        MeasureElement(body, 10, ref maxW, ref totalH);
        return (maxW, totalH);
    }

    /// <summary>
    /// Measures the dimensions required to render an HTML element, including its child elements.
    /// Updates the maximum width and total height based on the provided element and its structure.
    /// </summary>
    /// <param name="element">The HTML element to measure.</param>
    /// <param name="x">The horizontal offset, starting from the leftmost position of the element.</param>
    /// <param name="maxWidth">A reference to the maximum width observed, which will be updated if a new larger width is encountered.</param>
    /// <param name="height">A reference to the cumulative height, which will be updated based on the height of the element and its children.</param>
    private void MeasureElement(IElement element, float x, ref int maxWidth, ref int height)
    {
        using var measurePaint = new SKPaint { TextSize = 16 }; // Generic

        foreach (var child in element.Children)
        {
            if (child is IText textNode)
            {
                var textBounds = new SKRect();
                measurePaint.MeasureText(textNode.Text, ref textBounds);
                maxWidth = Math.Max(maxWidth, (int)(x + textBounds.Width + 20));
                height += (int)measurePaint.TextSize + 4;
            }
            else if (child.TagName == "P" || child.TagName == "DIV")
            {
                MeasureElement(child, x, ref maxWidth, ref height);
                height += 10;
            }
            else if (child.TagName == "PRE")
            {
                var lines = child.TextContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    var textBounds = new SKRect();
                    measurePaint.MeasureText(line, ref textBounds);
                    maxWidth = Math.Max(maxWidth, (int)(x + textBounds.Width + 20));
                    height += (int)measurePaint.TextSize + 4;
                }

                height += 10;
            }
            else if (child.TagName == "TABLE")
            {
                var rows = child.QuerySelectorAll("tr").ToList();
                if (!rows.Any()) continue;

                var colCount = rows.Max(r => r.Children.Length);
                float tableWidth = 0;
                var tableHeight = 0;

                for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
                {
                    var cells = rows[rowIdx].Children.Where(c => c.TagName == "TD" || c.TagName == "TH").ToList();
                    float rowHeight = 20;
                    float rowWidth = 0;

                    for (var colIdx = 0; colIdx < cells.Count; colIdx++)
                    {
                        var cellText = cells[colIdx].TextContent.Trim();
                        var textBounds = new SKRect();
                        measurePaint.MeasureText(cellText, ref textBounds);
                        var colWidth = textBounds.Width + 20;
                        rowWidth += colWidth;
                        rowHeight = Math.Max(rowHeight, textBounds.Height + 20);
                    }

                    tableWidth = Math.Max(tableWidth, rowWidth + colCount);
                    tableHeight += (int)rowHeight;
                }

                maxWidth = Math.Max(maxWidth, (int)(x + tableWidth));
                height += tableHeight + 20;
            }
        }
    }
}