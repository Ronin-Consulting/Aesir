using Aesir.Client.Web.Infrastructure.Services;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class MarkdownServiceTests
{
    private readonly MarkdownService _sut;

    public MarkdownServiceTests()
    {
        _sut = new MarkdownService();
    }

    [Fact]
    public void ToHtml_ReturnsEmptyString_WhenInputIsNull()
    {
        // Act
        var result = _sut.ToHtml(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToHtml_ReturnsEmptyString_WhenInputIsEmpty()
    {
        // Act
        var result = _sut.ToHtml("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToHtml_RendersBoldText()
    {
        // Arrange
        var markdown = "This is **bold** text";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void ToHtml_RendersItalicText()
    {
        // Arrange
        var markdown = "This is *italic* text";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void ToHtml_RendersInlineCode()
    {
        // Arrange
        var markdown = "Use `code` here";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<code>code</code>");
    }

    [Fact]
    public void ToHtml_RendersCodeBlock()
    {
        // Arrange
        var markdown = "```\nvar x = 1;\n```";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert - Code blocks have copy button wrapper
        result.Should().Contain("<pre>");
        result.Should().Contain("<code id=");
        result.Should().Contain("code-copy-btn");
    }

    [Fact]
    public void ToHtml_RendersUnorderedList()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>");
    }

    [Fact]
    public void ToHtml_RendersOrderedList()
    {
        // Arrange
        var markdown = "1. First\n2. Second\n3. Third";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<ol>");
        result.Should().Contain("<li>");
    }

    [Fact]
    public void ToHtml_RendersLinks()
    {
        // Arrange
        var markdown = "Check [this link](https://example.com)";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<a href=\"https://example.com\"");
        result.Should().Contain("this link</a>");
    }

    [Fact]
    public void ToHtml_RendersAutoLinks()
    {
        // Arrange
        var markdown = "Visit https://example.com for more";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<a href=\"https://example.com\"");
    }

    [Fact]
    public void ToHtml_RendersBlockquotes()
    {
        // Arrange
        var markdown = "> This is a quote";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<blockquote>");
    }

    [Fact]
    public void ToHtml_RendersHeaders()
    {
        // Arrange
        var markdown = "# Header 1\n## Header 2\n### Header 3";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<h1");
        result.Should().Contain("<h2");
        result.Should().Contain("<h3");
    }

    [Fact]
    public void ToHtml_RendersHorizontalRule()
    {
        // Arrange
        var markdown = "Above\n\n---\n\nBelow";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<hr");
    }

    [Fact]
    public void ToHtml_RendersTaskLists()
    {
        // Arrange
        var markdown = "- [ ] Unchecked\n- [x] Checked";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("type=\"checkbox\"");
    }

    [Fact]
    public void ToHtml_RendersTable()
    {
        // Arrange
        var markdown = "| Col 1 | Col 2 |\n|-------|-------|\n| A | B |";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<table>");
        result.Should().Contain("<th>");
        result.Should().Contain("<td>");
    }

    [Fact]
    public void ToHtml_PreservesPlainText()
    {
        // Arrange
        var markdown = "Just plain text here.";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("Just plain text here.");
    }

    [Fact]
    public void ToHtml_HandlesMultipleParagraphs()
    {
        // Arrange
        var markdown = "Paragraph 1.\n\nParagraph 2.";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<p>Paragraph 1.</p>");
        result.Should().Contain("<p>Paragraph 2.</p>");
    }

    [Fact]
    public void ToHtml_RendersStrikethrough()
    {
        // Arrange
        var markdown = "This is ~~strikethrough~~ text";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.Should().Contain("<del>strikethrough</del>");
    }

    #region ToHtmlPreservingWhitespace Tests

    [Fact]
    public void ToHtmlPreservingWhitespace_ReturnsEmptyString_WhenInputIsNull()
    {
        // Act
        var result = _sut.ToHtmlPreservingWhitespace(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_ReturnsEmptyString_WhenInputIsEmpty()
    {
        // Act
        var result = _sut.ToHtmlPreservingWhitespace("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_ConvertsSingleNewlineToBr()
    {
        // Arrange
        var markdown = "Line 1\nLine 2";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert - Single newline should become <br /> due to UseSoftlineBreakAsHardlineBreak
        result.Should().Contain("<br");
        result.Should().Contain("Line 1");
        result.Should().Contain("Line 2");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_PreservesMultipleSpaces()
    {
        // Arrange
        var markdown = "Word    with    spaces";  // Multiple spaces

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert - Should contain non-breaking spaces to preserve spacing
        result.Should().Contain("\u00A0");  // Non-breaking space
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_StillRendersBoldText()
    {
        // Arrange
        var markdown = "This is **bold** text";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_StillRendersItalicText()
    {
        // Arrange
        var markdown = "This is *italic* text";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_RendersCodeBlocks()
    {
        // Arrange
        var markdown = "```\ncode here\n```";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<pre>");
        result.Should().Contain("<code");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_RendersInlineCode()
    {
        // Arrange
        var markdown = "Use `code` here";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<code>code</code>");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_RendersList()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_RendersLinks()
    {
        // Arrange
        var markdown = "Check [this link](https://example.com)";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<a href=\"https://example.com\"");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_PreservesMultipleNewlines()
    {
        // Arrange
        var markdown = "Line 1\n\n\nLine 2";  // Three newlines (double paragraph break)

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert - Should preserve the visual separation
        result.Should().Contain("Line 1");
        result.Should().Contain("Line 2");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_RendersBlockquotes()
    {
        // Arrange
        var markdown = "> This is a quote";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert
        result.Should().Contain("<blockquote>");
    }

    [Fact]
    public void ToHtmlPreservingWhitespace_PreservesTabCharacters()
    {
        // Arrange
        var markdown = "Column1\tColumn2";

        // Act
        var result = _sut.ToHtmlPreservingWhitespace(markdown);

        // Assert - Tabs should be converted to non-breaking spaces
        result.Should().Contain("\u00A0");  // Non-breaking space
        result.Should().Contain("Column1");
        result.Should().Contain("Column2");
    }

    #endregion
}
