using MudBlazor;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Helper service for consistent file type icon and color mapping across all file display components.
/// </summary>
public static class FileTypeHelper
{
    /// <summary>
    /// File type categories for styling purposes.
    /// </summary>
    public enum FileCategory
    {
        Pdf,
        Image,
        Document,
        Data,
        Code,
        Archive,
        Audio,
        Video,
        Other
    }

    /// <summary>
    /// File type styling information.
    /// </summary>
    public record FileTypeStyle(
        string Icon,
        string AccentColor,
        string BackgroundColor,
        FileCategory Category,
        string Label);

    private static readonly Dictionary<string, FileTypeStyle> ExtensionStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        // PDF
        { ".pdf", new(Icons.Material.Filled.PictureAsPdf, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Pdf, "PDF") },

        // Images
        { ".png", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "PNG") },
        { ".jpg", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "JPG") },
        { ".jpeg", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "JPEG") },
        { ".gif", new(Icons.Material.Filled.Gif, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "GIF") },
        { ".webp", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "WebP") },
        { ".bmp", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "BMP") },
        { ".svg", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "SVG") },
        { ".tiff", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "TIFF") },
        { ".tif", new(Icons.Material.Filled.Image, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Image, "TIFF") },

        // Documents
        { ".doc", new(Icons.Material.Filled.Article, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Document, "DOC") },
        { ".docx", new(Icons.Material.Filled.Article, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Document, "DOCX") },
        { ".txt", new(Icons.Material.Filled.Description, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Document, "TXT") },
        { ".rtf", new(Icons.Material.Filled.Article, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Document, "RTF") },
        { ".odt", new(Icons.Material.Filled.Article, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Document, "ODT") },

        // Presentations
        { ".ppt", new(Icons.Material.Filled.Slideshow, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Document, "PPT") },
        { ".pptx", new(Icons.Material.Filled.Slideshow, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Document, "PPTX") },

        // Spreadsheets
        { ".xls", new(Icons.Material.Filled.TableChart, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Data, "XLS") },
        { ".xlsx", new(Icons.Material.Filled.TableChart, "#43A047", "rgba(67, 160, 71, 0.1)", FileCategory.Data, "XLSX") },

        // Data formats
        { ".json", new(Icons.Material.Filled.DataObject, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Data, "JSON") },
        { ".xml", new(Icons.Material.Filled.Code, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Data, "XML") },
        { ".csv", new(Icons.Material.Filled.TableChart, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Data, "CSV") },
        { ".yaml", new(Icons.Material.Filled.DataObject, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Data, "YAML") },
        { ".yml", new(Icons.Material.Filled.DataObject, "#FB8C00", "rgba(251, 140, 0, 0.1)", FileCategory.Data, "YAML") },

        // Code
        { ".cs", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "C#") },
        { ".js", new(Icons.Material.Filled.Javascript, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "JS") },
        { ".ts", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "TS") },
        { ".py", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Python") },
        { ".java", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Java") },
        { ".cpp", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "C++") },
        { ".c", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "C") },
        { ".h", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Header") },
        { ".rs", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Rust") },
        { ".go", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Go") },
        { ".rb", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Ruby") },
        { ".php", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "PHP") },
        { ".swift", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Swift") },
        { ".kt", new(Icons.Material.Filled.Code, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Kotlin") },
        { ".sql", new(Icons.Material.Filled.Storage, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "SQL") },
        { ".sh", new(Icons.Material.Filled.Terminal, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Shell") },
        { ".bash", new(Icons.Material.Filled.Terminal, "#8E24AA", "rgba(142, 36, 170, 0.1)", FileCategory.Code, "Bash") },

        // Markup
        { ".html", new(Icons.Material.Filled.Html, "#E65100", "rgba(230, 81, 0, 0.1)", FileCategory.Code, "HTML") },
        { ".htm", new(Icons.Material.Filled.Html, "#E65100", "rgba(230, 81, 0, 0.1)", FileCategory.Code, "HTML") },
        { ".css", new(Icons.Material.Filled.Css, "#1565C0", "rgba(21, 101, 192, 0.1)", FileCategory.Code, "CSS") },
        { ".scss", new(Icons.Material.Filled.Css, "#C2185B", "rgba(194, 24, 91, 0.1)", FileCategory.Code, "SCSS") },
        { ".md", new(Icons.Material.Filled.Description, "#546E7A", "rgba(84, 110, 122, 0.1)", FileCategory.Document, "Markdown") },
        { ".markdown", new(Icons.Material.Filled.Description, "#546E7A", "rgba(84, 110, 122, 0.1)", FileCategory.Document, "Markdown") },

        // Archives
        { ".zip", new(Icons.Material.Filled.FolderZip, "#6D4C41", "rgba(109, 76, 65, 0.1)", FileCategory.Archive, "ZIP") },
        { ".rar", new(Icons.Material.Filled.FolderZip, "#6D4C41", "rgba(109, 76, 65, 0.1)", FileCategory.Archive, "RAR") },
        { ".7z", new(Icons.Material.Filled.FolderZip, "#6D4C41", "rgba(109, 76, 65, 0.1)", FileCategory.Archive, "7Z") },
        { ".tar", new(Icons.Material.Filled.FolderZip, "#6D4C41", "rgba(109, 76, 65, 0.1)", FileCategory.Archive, "TAR") },
        { ".gz", new(Icons.Material.Filled.FolderZip, "#6D4C41", "rgba(109, 76, 65, 0.1)", FileCategory.Archive, "GZ") },

        // Audio
        { ".mp3", new(Icons.Material.Filled.AudioFile, "#00897B", "rgba(0, 137, 123, 0.1)", FileCategory.Audio, "MP3") },
        { ".wav", new(Icons.Material.Filled.AudioFile, "#00897B", "rgba(0, 137, 123, 0.1)", FileCategory.Audio, "WAV") },
        { ".ogg", new(Icons.Material.Filled.AudioFile, "#00897B", "rgba(0, 137, 123, 0.1)", FileCategory.Audio, "OGG") },
        { ".flac", new(Icons.Material.Filled.AudioFile, "#00897B", "rgba(0, 137, 123, 0.1)", FileCategory.Audio, "FLAC") },

        // Video
        { ".mp4", new(Icons.Material.Filled.VideoFile, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Video, "MP4") },
        { ".avi", new(Icons.Material.Filled.VideoFile, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Video, "AVI") },
        { ".mkv", new(Icons.Material.Filled.VideoFile, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Video, "MKV") },
        { ".mov", new(Icons.Material.Filled.VideoFile, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Video, "MOV") },
        { ".webm", new(Icons.Material.Filled.VideoFile, "#1E88E5", "rgba(30, 136, 229, 0.1)", FileCategory.Video, "WebM") },

        // Config
        { ".config", new(Icons.Material.Filled.Settings, "#757575", "rgba(117, 117, 117, 0.1)", FileCategory.Data, "Config") },
        { ".env", new(Icons.Material.Filled.Settings, "#757575", "rgba(117, 117, 117, 0.1)", FileCategory.Data, "Env") },
        { ".ini", new(Icons.Material.Filled.Settings, "#757575", "rgba(117, 117, 117, 0.1)", FileCategory.Data, "INI") },
    };

    private static readonly FileTypeStyle DefaultStyle = new(
        Icons.Material.Filled.InsertDriveFile,
        "#757575",
        "rgba(117, 117, 117, 0.1)",
        FileCategory.Other,
        "File");

    /// <summary>
    /// Gets the file type style for a given filename or extension.
    /// </summary>
    public static FileTypeStyle GetStyle(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return DefaultStyle;

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
            return DefaultStyle;

        return ExtensionStyles.TryGetValue(extension, out var style) ? style : DefaultStyle;
    }

    /// <summary>
    /// Gets just the icon for a given filename.
    /// </summary>
    public static string GetIcon(string? fileName) => GetStyle(fileName).Icon;

    /// <summary>
    /// Gets just the accent color for a given filename.
    /// </summary>
    public static string GetAccentColor(string? fileName) => GetStyle(fileName).AccentColor;

    /// <summary>
    /// Gets just the background color for a given filename.
    /// </summary>
    public static string GetBackgroundColor(string? fileName) => GetStyle(fileName).BackgroundColor;

    /// <summary>
    /// Gets the file category for a given filename.
    /// </summary>
    public static FileCategory GetCategory(string? fileName) => GetStyle(fileName).Category;

    /// <summary>
    /// Gets the short label for a file type (e.g., "PDF", "PNG").
    /// </summary>
    public static string GetLabel(string? fileName) => GetStyle(fileName).Label;

    /// <summary>
    /// Checks if the file is an image type.
    /// </summary>
    public static bool IsImage(string? fileName) => GetCategory(fileName) == FileCategory.Image;

    /// <summary>
    /// Checks if the file is a PDF.
    /// </summary>
    public static bool IsPdf(string? fileName) => GetCategory(fileName) == FileCategory.Pdf;

    /// <summary>
    /// Checks if the file is a text-based file that can be previewed.
    /// </summary>
    public static bool IsTextBased(string? fileName)
    {
        var category = GetCategory(fileName);
        return category is FileCategory.Document or FileCategory.Code or FileCategory.Data;
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return string.Empty;

        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return order == 0 ? $"{size:0} {sizes[order]}" : $"{size:0.#} {sizes[order]}";
    }

    /// <summary>
    /// Truncates a filename with ellipsis, keeping the extension visible.
    /// Uses middle ellipsis to preserve both start and extension.
    /// </summary>
    public static string TruncateFileName(string? fileName, int maxLength = 25)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length <= maxLength)
            return fileName ?? string.Empty;

        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Ensure we have room for at least some characters + ellipsis + extension
        var availableForName = maxLength - extension.Length - 3; // 3 for "..."
        if (availableForName < 4)
        {
            // File name is very short, just truncate from end
            return fileName[..(maxLength - 3)] + "...";
        }

        // Split available space between start and end of name
        var startChars = availableForName / 2;
        var endChars = availableForName - startChars;

        var truncatedName = nameWithoutExtension[..startChars] + "..." +
                           nameWithoutExtension[^endChars..];

        return truncatedName + extension;
    }
}
