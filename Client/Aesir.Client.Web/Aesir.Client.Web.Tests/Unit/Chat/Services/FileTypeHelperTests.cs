using Aesir.Client.Web.Infrastructure.Services;
using MudBlazor;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class FileTypeHelperTests
{
    #region GetStyle Tests

    [Fact]
    public void GetStyle_ReturnsDefaultStyle_WhenFileNameIsNull()
    {
        // Act
        var result = FileTypeHelper.GetStyle(null);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Other);
        result.Label.Should().Be("File");
    }

    [Fact]
    public void GetStyle_ReturnsDefaultStyle_WhenFileNameIsEmpty()
    {
        // Act
        var result = FileTypeHelper.GetStyle("");

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Other);
        result.Label.Should().Be("File");
    }

    [Fact]
    public void GetStyle_ReturnsDefaultStyle_WhenFileNameIsWhitespace()
    {
        // Act
        var result = FileTypeHelper.GetStyle("   ");

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Other);
        result.Label.Should().Be("File");
    }

    [Fact]
    public void GetStyle_ReturnsDefaultStyle_WhenFileHasNoExtension()
    {
        // Act
        var result = FileTypeHelper.GetStyle("filename");

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Other);
        result.Label.Should().Be("File");
    }

    [Fact]
    public void GetStyle_ReturnsDefaultStyle_WhenExtensionIsUnknown()
    {
        // Act
        var result = FileTypeHelper.GetStyle("file.xyz");

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Other);
        result.Label.Should().Be("File");
    }

    #endregion

    #region PDF Tests

    [Fact]
    public void GetStyle_ReturnsPdfStyle_ForPdfFile()
    {
        // Act
        var result = FileTypeHelper.GetStyle("document.pdf");

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Pdf);
        result.Label.Should().Be("PDF");
        result.Icon.Should().Be(Icons.Material.Filled.PictureAsPdf);
        result.AccentColor.Should().Be("#1E88E5");
    }

    [Fact]
    public void GetStyle_IsCaseInsensitive_ForPdfExtension()
    {
        // Act
        var lowerResult = FileTypeHelper.GetStyle("file.pdf");
        var upperResult = FileTypeHelper.GetStyle("file.PDF");
        var mixedResult = FileTypeHelper.GetStyle("file.Pdf");

        // Assert
        lowerResult.Category.Should().Be(FileTypeHelper.FileCategory.Pdf);
        upperResult.Category.Should().Be(FileTypeHelper.FileCategory.Pdf);
        mixedResult.Category.Should().Be(FileTypeHelper.FileCategory.Pdf);
    }

    #endregion

    #region Image Tests

    [Theory]
    [InlineData("image.png", "PNG")]
    [InlineData("photo.jpg", "JPG")]
    [InlineData("picture.jpeg", "JPEG")]
    [InlineData("animation.gif", "GIF")]
    [InlineData("image.webp", "WebP")]
    [InlineData("bitmap.bmp", "BMP")]
    [InlineData("vector.svg", "SVG")]
    [InlineData("scan.tiff", "TIFF")]
    [InlineData("scan.tif", "TIFF")]
    public void GetStyle_ReturnsImageCategory_ForImageFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Image);
        result.Label.Should().Be(expectedLabel);
    }

    [Fact]
    public void IsImage_ReturnsTrue_ForImageFile()
    {
        // Act & Assert
        FileTypeHelper.IsImage("photo.jpg").Should().BeTrue();
        FileTypeHelper.IsImage("image.png").Should().BeTrue();
        FileTypeHelper.IsImage("animation.gif").Should().BeTrue();
    }

    [Fact]
    public void IsImage_ReturnsFalse_ForNonImageFile()
    {
        // Act & Assert
        FileTypeHelper.IsImage("document.pdf").Should().BeFalse();
        FileTypeHelper.IsImage("script.js").Should().BeFalse();
        FileTypeHelper.IsImage("data.json").Should().BeFalse();
    }

    #endregion

    #region Document Tests

    [Theory]
    [InlineData("document.doc", "DOC")]
    [InlineData("document.docx", "DOCX")]
    [InlineData("readme.txt", "TXT")]
    [InlineData("document.rtf", "RTF")]
    [InlineData("document.odt", "ODT")]
    [InlineData("readme.md", "Markdown")]
    [InlineData("readme.markdown", "Markdown")]
    public void GetStyle_ReturnsDocumentCategory_ForDocumentFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Document);
        result.Label.Should().Be(expectedLabel);
    }

    #endregion

    #region Code Tests

    [Theory]
    [InlineData("program.cs", "C#")]
    [InlineData("script.js", "JS")]
    [InlineData("module.ts", "TS")]
    [InlineData("script.py", "Python")]
    [InlineData("Main.java", "Java")]
    [InlineData("main.cpp", "C++")]
    [InlineData("main.c", "C")]
    [InlineData("header.h", "Header")]
    [InlineData("main.rs", "Rust")]
    [InlineData("main.go", "Go")]
    [InlineData("script.rb", "Ruby")]
    [InlineData("index.php", "PHP")]
    [InlineData("app.swift", "Swift")]
    [InlineData("Main.kt", "Kotlin")]
    [InlineData("query.sql", "SQL")]
    [InlineData("script.sh", "Shell")]
    [InlineData("script.bash", "Bash")]
    [InlineData("page.html", "HTML")]
    [InlineData("page.htm", "HTML")]
    [InlineData("style.css", "CSS")]
    [InlineData("style.scss", "SCSS")]
    public void GetStyle_ReturnsCodeCategory_ForCodeFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Code);
        result.Label.Should().Be(expectedLabel);
    }

    #endregion

    #region Data Tests

    [Theory]
    [InlineData("data.json", "JSON")]
    [InlineData("config.xml", "XML")]
    [InlineData("data.csv", "CSV")]
    [InlineData("config.yaml", "YAML")]
    [InlineData("config.yml", "YAML")]
    [InlineData("spreadsheet.xls", "XLS")]
    [InlineData("spreadsheet.xlsx", "XLSX")]
    [InlineData("app.config", "Config")]
    [InlineData(".env", "Env")]
    [InlineData("settings.ini", "INI")]
    public void GetStyle_ReturnsDataCategory_ForDataFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Data);
        result.Label.Should().Be(expectedLabel);
    }

    #endregion

    #region Archive Tests

    [Theory]
    [InlineData("archive.zip", "ZIP")]
    [InlineData("archive.rar", "RAR")]
    [InlineData("archive.7z", "7Z")]
    [InlineData("archive.tar", "TAR")]
    [InlineData("archive.gz", "GZ")]
    public void GetStyle_ReturnsArchiveCategory_ForArchiveFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Archive);
        result.Label.Should().Be(expectedLabel);
    }

    #endregion

    #region Audio Tests

    [Theory]
    [InlineData("song.mp3", "MP3")]
    [InlineData("sound.wav", "WAV")]
    [InlineData("audio.ogg", "OGG")]
    [InlineData("music.flac", "FLAC")]
    public void GetStyle_ReturnsAudioCategory_ForAudioFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Audio);
        result.Label.Should().Be(expectedLabel);
    }

    #endregion

    #region Video Tests

    [Theory]
    [InlineData("video.mp4", "MP4")]
    [InlineData("movie.avi", "AVI")]
    [InlineData("video.mkv", "MKV")]
    [InlineData("clip.mov", "MOV")]
    [InlineData("video.webm", "WebM")]
    public void GetStyle_ReturnsVideoCategory_ForVideoFiles(string fileName, string expectedLabel)
    {
        // Act
        var result = FileTypeHelper.GetStyle(fileName);

        // Assert
        result.Category.Should().Be(FileTypeHelper.FileCategory.Video);
        result.Label.Should().Be(expectedLabel);
    }

    #endregion

    #region IsPdf Tests

    [Fact]
    public void IsPdf_ReturnsTrue_ForPdfFile()
    {
        // Act & Assert
        FileTypeHelper.IsPdf("document.pdf").Should().BeTrue();
    }

    [Fact]
    public void IsPdf_ReturnsFalse_ForNonPdfFile()
    {
        // Act & Assert
        FileTypeHelper.IsPdf("image.png").Should().BeFalse();
        FileTypeHelper.IsPdf("document.docx").Should().BeFalse();
    }

    #endregion

    #region IsTextBased Tests

    [Theory]
    [InlineData("document.txt")]
    [InlineData("document.docx")]
    [InlineData("readme.md")]
    [InlineData("script.js")]
    [InlineData("program.cs")]
    [InlineData("data.json")]
    [InlineData("config.yaml")]
    public void IsTextBased_ReturnsTrue_ForTextBasedFiles(string fileName)
    {
        // Act & Assert
        FileTypeHelper.IsTextBased(fileName).Should().BeTrue();
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("image.png")]
    [InlineData("archive.zip")]
    [InlineData("video.mp4")]
    [InlineData("audio.mp3")]
    public void IsTextBased_ReturnsFalse_ForNonTextBasedFiles(string fileName)
    {
        // Act & Assert
        FileTypeHelper.IsTextBased(fileName).Should().BeFalse();
    }

    #endregion

    #region GetIcon Tests

    [Fact]
    public void GetIcon_ReturnsCorrectIcon_ForPdf()
    {
        // Act
        var icon = FileTypeHelper.GetIcon("document.pdf");

        // Assert
        icon.Should().Be(Icons.Material.Filled.PictureAsPdf);
    }

    [Fact]
    public void GetIcon_ReturnsDefaultIcon_ForUnknownExtension()
    {
        // Act
        var icon = FileTypeHelper.GetIcon("file.unknown");

        // Assert
        icon.Should().Be(Icons.Material.Filled.InsertDriveFile);
    }

    #endregion

    #region GetAccentColor Tests

    [Fact]
    public void GetAccentColor_ReturnsCorrectColor_ForPdf()
    {
        // Act
        var color = FileTypeHelper.GetAccentColor("document.pdf");

        // Assert
        color.Should().Be("#1E88E5");
    }

    [Fact]
    public void GetAccentColor_ReturnsDefaultColor_ForUnknownExtension()
    {
        // Act
        var color = FileTypeHelper.GetAccentColor("file.unknown");

        // Assert
        color.Should().Be("#757575");
    }

    #endregion

    #region GetBackgroundColor Tests

    [Fact]
    public void GetBackgroundColor_ReturnsCorrectColor_ForPdf()
    {
        // Act
        var color = FileTypeHelper.GetBackgroundColor("document.pdf");

        // Assert
        color.Should().Be("rgba(30, 136, 229, 0.1)");
    }

    #endregion

    #region GetCategory Tests

    [Fact]
    public void GetCategory_ReturnsCorrectCategory_ForPdf()
    {
        // Act
        var category = FileTypeHelper.GetCategory("document.pdf");

        // Assert
        category.Should().Be(FileTypeHelper.FileCategory.Pdf);
    }

    #endregion

    #region GetLabel Tests

    [Fact]
    public void GetLabel_ReturnsCorrectLabel_ForPdf()
    {
        // Act
        var label = FileTypeHelper.GetLabel("document.pdf");

        // Assert
        label.Should().Be("PDF");
    }

    [Fact]
    public void GetLabel_ReturnsDefaultLabel_ForUnknownExtension()
    {
        // Act
        var label = FileTypeHelper.GetLabel("file.unknown");

        // Assert
        label.Should().Be("File");
    }

    #endregion

    #region FormatFileSize Tests

    [Fact]
    public void FormatFileSize_ReturnsEmpty_WhenBytesIsZero()
    {
        // Act
        var result = FileTypeHelper.FormatFileSize(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatFileSize_ReturnsEmpty_WhenBytesIsNegative()
    {
        // Act
        var result = FileTypeHelper.FormatFileSize(-100);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(100, "100 B")]
    [InlineData(999, "999 B")]
    public void FormatFileSize_ReturnsBytes_ForSmallSizes(long bytes, string expected)
    {
        // Act
        var result = FileTypeHelper.FormatFileSize(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(2048, "2 KB")]
    [InlineData(10240, "10 KB")]
    public void FormatFileSize_ReturnsKilobytes_ForKBSizes(long bytes, string expected)
    {
        // Act
        var result = FileTypeHelper.FormatFileSize(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1048576, "1 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(10485760, "10 MB")]
    public void FormatFileSize_ReturnsMegabytes_ForMBSizes(long bytes, string expected)
    {
        // Act
        var result = FileTypeHelper.FormatFileSize(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1610612736, "1.5 GB")]
    public void FormatFileSize_ReturnsGigabytes_ForGBSizes(long bytes, string expected)
    {
        // Act
        var result = FileTypeHelper.FormatFileSize(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatFileSize_ReturnsTerabytes_ForTBSizes()
    {
        // Arrange
        long bytes = 1099511627776; // 1 TB

        // Act
        var result = FileTypeHelper.FormatFileSize(bytes);

        // Assert
        result.Should().Be("1 TB");
    }

    #endregion

    #region TruncateFileName Tests

    [Fact]
    public void TruncateFileName_ReturnsEmpty_WhenFileNameIsNull()
    {
        // Act
        var result = FileTypeHelper.TruncateFileName(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void TruncateFileName_ReturnsEmpty_WhenFileNameIsEmpty()
    {
        // Act
        var result = FileTypeHelper.TruncateFileName("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void TruncateFileName_ReturnsOriginal_WhenShorterThanMaxLength()
    {
        // Arrange
        var fileName = "short.txt";

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName, 25);

        // Assert
        result.Should().Be(fileName);
    }

    [Fact]
    public void TruncateFileName_ReturnsOriginal_WhenEqualToMaxLength()
    {
        // Arrange
        var fileName = "exactly-twenty-five.txt"; // 23 chars

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName, 23);

        // Assert
        result.Should().Be(fileName);
    }

    [Fact]
    public void TruncateFileName_TruncatesWithMiddleEllipsis_WhenLongerThanMaxLength()
    {
        // Arrange
        var fileName = "this-is-a-very-long-filename-that-needs-truncation.pdf";

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName, 25);

        // Assert
        result.Should().HaveLength(25);
        result.Should().Contain("...");
        result.Should().EndWith(".pdf");
    }

    [Fact]
    public void TruncateFileName_PreservesExtension_WhenTruncating()
    {
        // Arrange
        var fileName = "very-long-document-name.docx";

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName, 20);

        // Assert
        result.Should().EndWith(".docx");
    }

    [Fact]
    public void TruncateFileName_HandlesVeryShortMaxLength()
    {
        // Arrange
        var fileName = "document.pdf";

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName, 8);

        // Assert
        result.Should().HaveLength(8);
        result.Should().EndWith("...");
    }

    [Fact]
    public void TruncateFileName_UsesDefaultMaxLength_WhenNotSpecified()
    {
        // Arrange - 30 character filename
        var fileName = "this-is-exactly-thirty-chars.txt";

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName);

        // Assert
        // Default is 25, so 30-char name should be truncated
        result.Should().HaveLength(25);
    }

    [Fact]
    public void TruncateFileName_KeepsStartAndEndOfName()
    {
        // Arrange
        var fileName = "important-document-report-2024.pdf";

        // Act
        var result = FileTypeHelper.TruncateFileName(fileName, 25);

        // Assert
        result.Should().StartWith("import");
        result.Should().Contain("...");
        result.Should().EndWith(".pdf");
    }

    #endregion
}
