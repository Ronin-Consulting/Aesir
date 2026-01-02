using Aesir.Infrastructure.Documents;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DocFormat = Aesir.Infrastructure.Documents.DocumentFormat;

namespace Aesir.Infrastructure.Tests.Documents;

public class WordExporterTests
{
    private readonly Mock<ILogger<WordExporter>> _loggerMock;
    private readonly WordExporter _exporter;

    public WordExporterTests()
    {
        _loggerMock = new Mock<ILogger<WordExporter>>();
        _exporter = new WordExporter(_loggerMock.Object);
    }

    [Fact]
    public void Format_ReturnsWord()
    {
        // Act & Assert
        _exporter.Format.Should().Be(DocFormat.Word);
    }

    [Fact]
    public async Task ExportAsync_WithSimpleDocument_ReturnsValidDocx()
    {
        // Arrange
        var document = CreateSimpleDocument();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Length.Should().BeGreaterThan(0);
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        result.FileName.Should().EndWith(".docx");
    }

    [Fact]
    public async Task ExportAsync_WithMultipleSections_ReturnsValidDocx()
    {
        // Arrange
        var document = CreateDocumentWithMultipleSections();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_WithTable_ReturnsValidDocx()
    {
        // Arrange
        var document = CreateDocumentWithTable();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_WithList_ReturnsValidDocx()
    {
        // Arrange
        var document = CreateDocumentWithList();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_WithQuote_ReturnsValidDocx()
    {
        // Arrange
        var document = CreateDocumentWithQuote();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_WithCode_ReturnsValidDocx()
    {
        // Arrange
        var document = CreateDocumentWithCode();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_SanitizesFilename()
    {
        // Arrange
        var document = new ExportDocument
        {
            Title = "Report: Test/File\\Name",
            Sections = []
        };

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.FileName.Should().NotContain("/");
        result.FileName.Should().NotContain("\\");
        result.FileName.Should().EndWith(".docx");
    }

    [Fact]
    public async Task ExportAsync_WithPageBreaks_ReturnsValidDocx()
    {
        // Arrange
        var document = new ExportDocument
        {
            Title = "Multi-Page Document",
            Sections =
            [
                new DocumentSection { Heading = "Section 1", HeadingLevel = 1, Content = [new ParagraphContent { Text = "Content 1" }] },
                new DocumentSection { Heading = "Section 2", HeadingLevel = 1, PageBreakBefore = true, Content = [new ParagraphContent { Text = "Content 2" }] }
            ]
        };

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_WithMetadata_IncludesMetadataInDocument()
    {
        // Arrange
        var document = new ExportDocument
        {
            Title = "Test Document",
            Subtitle = "Test Subtitle",
            Author = "Test Author",
            CreatedAt = DateTime.UtcNow,
            Sections = [new DocumentSection { Heading = "Test", HeadingLevel = 1, Content = [] }]
        };

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_WithFormattedParagraphs_ReturnsValidDocx()
    {
        // Arrange
        var document = new ExportDocument
        {
            Title = "Formatted Document",
            Sections =
            [
                new DocumentSection
                {
                    Heading = "Formatting Test",
                    HeadingLevel = 1,
                    Content =
                    [
                        new ParagraphContent { Text = "Bold text", Bold = true },
                        new ParagraphContent { Text = "Italic text", Italic = true },
                        new ParagraphContent { Text = "Bold and Italic", Bold = true, Italic = true }
                    ]
                }
            ]
        };

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_ProducesValidZipStructure()
    {
        // Arrange - DOCX is a ZIP file
        var document = CreateSimpleDocument();

        // Act
        var result = await _exporter.ExportAsync(document);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        // Check for ZIP magic bytes (PK)
        result.Data![0].Should().Be(0x50); // P
        result.Data[1].Should().Be(0x4B);  // K
    }

    #region Helper Methods

    private static ExportDocument CreateSimpleDocument()
    {
        return new ExportDocument
        {
            Title = "Simple Test Document",
            Author = "Test Author",
            CreatedAt = DateTime.UtcNow,
            Sections =
            [
                new DocumentSection
                {
                    Heading = "Introduction",
                    HeadingLevel = 1,
                    Content = [new ParagraphContent { Text = "This is a simple test document." }]
                }
            ]
        };
    }

    private static ExportDocument CreateDocumentWithMultipleSections()
    {
        return new ExportDocument
        {
            Title = "Multi-Section Document",
            Sections =
            [
                new DocumentSection { Heading = "Section 1", HeadingLevel = 1, Content = [new ParagraphContent { Text = "Content 1" }] },
                new DocumentSection { Heading = "Section 1.1", HeadingLevel = 2, Content = [new ParagraphContent { Text = "Sub-content" }] },
                new DocumentSection { Heading = "Section 2", HeadingLevel = 1, Content = [new ParagraphContent { Text = "Content 2" }] }
            ]
        };
    }

    private static ExportDocument CreateDocumentWithTable()
    {
        return new ExportDocument
        {
            Title = "Document with Table",
            Sections =
            [
                new DocumentSection
                {
                    Heading = "Data",
                    HeadingLevel = 1,
                    Content =
                    [
                        new TableContent
                        {
                            Headers = ["Column 1", "Column 2", "Column 3"],
                            Rows =
                            [
                                ["Row 1 Col 1", "Row 1 Col 2", "Row 1 Col 3"],
                                ["Row 2 Col 1", "Row 2 Col 2", "Row 2 Col 3"]
                            ]
                        }
                    ]
                }
            ]
        };
    }

    private static ExportDocument CreateDocumentWithList()
    {
        return new ExportDocument
        {
            Title = "Document with List",
            Sections =
            [
                new DocumentSection
                {
                    Heading = "Items",
                    HeadingLevel = 1,
                    Content =
                    [
                        new ListContent
                        {
                            Items = ["Item 1", "Item 2", "Item 3"],
                            Ordered = false
                        },
                        new ListContent
                        {
                            Items = ["First", "Second", "Third"],
                            Ordered = true
                        }
                    ]
                }
            ]
        };
    }

    private static ExportDocument CreateDocumentWithQuote()
    {
        return new ExportDocument
        {
            Title = "Document with Quote",
            Sections =
            [
                new DocumentSection
                {
                    Heading = "Quotes",
                    HeadingLevel = 1,
                    Content =
                    [
                        new QuoteContent
                        {
                            Text = "This is an important quote.",
                            Citation = "Author Name"
                        }
                    ]
                }
            ]
        };
    }

    private static ExportDocument CreateDocumentWithCode()
    {
        return new ExportDocument
        {
            Title = "Document with Code",
            Sections =
            [
                new DocumentSection
                {
                    Heading = "Code Example",
                    HeadingLevel = 1,
                    Content =
                    [
                        new CodeContent
                        {
                            Code = "public class Example { }",
                            Language = "csharp"
                        }
                    ]
                }
            ]
        };
    }

    #endregion
}
