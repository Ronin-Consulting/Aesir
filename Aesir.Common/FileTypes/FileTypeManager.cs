using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aesir.Common.FileTypes;

/// <summary>
/// Provides centralized management of file types, MIME types, and file validation logic.
/// This class serves as the single source of truth for all file type operations across the Aesir solution.
/// </summary>
public static class FileTypeManager
{
    #region File Type Categories
    
    /// <summary>
    /// Document file types supported by the system.
    /// </summary>
    public static class Documents
    {
        public const string Pdf = ".pdf";
        public const string Doc = ".doc";
        public const string Docx = ".docx";
        public const string Rtf = ".rtf";
        
        public static readonly string[] AllExtensions = { Pdf, Doc, Docx, Rtf };
    }
    
    /// <summary>
    /// Image file types supported by the system.
    /// </summary>
    public static class Images
    {
        public const string Png = ".png";
        public const string Jpg = ".jpg";
        public const string Jpeg = ".jpeg";
        public const string Gif = ".gif";
        public const string Bmp = ".bmp";
        public const string Tiff = ".tiff";
        public const string Tif = ".tif";
        public const string Svg = ".svg";
        
        public static readonly string[] AllExtensions = { Png, Jpg, Jpeg, Gif, Bmp, Tiff, Tif, Svg };
    }
    
    /// <summary>
    /// Text-based file types supported by the system.
    /// </summary>
    public static class Text
    {
        public const string PlainText = ".txt";
        public const string Html = ".html";
        public const string Htm = ".htm";
        public const string Markdown = ".md";
        public const string MarkdownFull = ".markdown";
        public const string Xml = ".xml";
        public const string Json = ".json";
        public const string Csv = ".csv";
        
        public static readonly string[] AllExtensions = { PlainText, Html, Htm, Markdown, MarkdownFull, Xml, Json, Csv };
    }
    
    /// <summary>
    /// Spreadsheet file types supported by the system.
    /// </summary>
    public static class Spreadsheets
    {
        public const string Xls = ".xls";
        public const string Xlsx = ".xlsx";
        
        public static readonly string[] AllExtensions = { Xls, Xlsx };
    }
    
    /// <summary>
    /// Presentation file types supported by the system.
    /// </summary>
    public static class Presentations
    {
        public const string Ppt = ".ppt";
        public const string Pptx = ".pptx";
        
        public static readonly string[] AllExtensions = { Ppt, Pptx };
    }
    
    /// <summary>
    /// Audio file types supported by the system.
    /// </summary>
    public static class Audio
    {
        public const string Mp3 = ".mp3";
        public const string Wav = ".wav";
        public const string Flac = ".flac";
        public const string Ogg = ".ogg";
        public const string Aac = ".aac";
        
        public static readonly string[] AllExtensions = { Mp3, Wav, Flac, Ogg, Aac };
    }
    
    /// <summary>
    /// Archive file types supported by the system.
    /// </summary>
    public static class Archives
    {
        public const string Zip = ".zip";
        public const string Rar = ".rar";
        public const string SevenZ = ".7z";
        public const string Tar = ".tar";
        public const string Gz = ".gz";
        
        public static readonly string[] AllExtensions = { Zip, Rar, SevenZ, Tar, Gz };
    }
    
    #endregion
    
    #region MIME Type Constants
    
    /// <summary>
    /// MIME types for document files.
    /// </summary>
    public static class MimeTypes
    {
        // Documents
        public const string Pdf = "application/pdf";
        public const string Doc = "application/msword";
        public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public const string Rtf = "application/rtf";
        
        // Images
        public const string Png = "image/png";
        public const string Jpeg = "image/jpeg";
        public const string Gif = "image/gif";
        public const string Bmp = "image/bmp";
        public const string Tiff = "image/tiff";
        public const string Svg = "image/svg+xml";
        
        // Text
        public const string PlainText = "text/plain";
        public const string Html = "text/html";
        public const string Markdown = "text/markdown";
        public const string Xml = "text/xml";
        public const string Json = "application/json";
        public const string Csv = "text/csv";
        
        // Spreadsheets
        public const string Xls = "application/vnd.ms-excel";
        public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        
        // Presentations
        public const string Ppt = "application/vnd.ms-powerpoint";
        public const string Pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        
        // Audio
        public const string Mp3 = "audio/mpeg";
        public const string Wav = "audio/wav";
        public const string Flac = "audio/flac";
        public const string Ogg = "audio/ogg";
        public const string Aac = "audio/aac";
        
        // Archives
        public const string Zip = "application/zip";
        public const string Rar = "application/vnd.rar";
        public const string SevenZ = "application/x-7z-compressed";
        public const string Tar = "application/x-tar";
        public const string Gzip = "application/gzip";
        
        // Default fallback
        public const string OctetStream = "application/octet-stream";
    }
    
    #endregion
    
    #region Apple Uniform Type Identifiers
    
    /// <summary>
    /// Apple Uniform Type Identifiers for various file types.
    /// </summary>
    public static class AppleUTIs
    {
        // Documents
        public const string Pdf = "com.adobe.pdf";
        public const string Doc = "com.microsoft.word.doc";
        public const string Docx = "org.openxmlformats.wordprocessingml.document";
        
        // Images
        public const string Png = "public.png";
        public const string Jpeg = "public.jpeg";
        public const string Gif = "com.compuserve.gif";
        
        // Text
        public const string PlainText = "public.plain-text";
        public const string Html = "public.html";
        public const string Markdown = "net.daringfireball.markdown";
        public const string Xml = "public.xml";
        public const string Json = "public.json";
        
        // Spreadsheets
        public const string Xlsx = "org.openxmlformats.spreadsheetml.sheet";
        
        // Presentations
        public const string Pptx = "org.openxmlformats.presentationml.presentation";
    }
    
    #endregion
    
    #region Extension to MIME Type Mapping
    
    /// <summary>
    /// Comprehensive mapping of file extensions to MIME types.
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionToMimeTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        { Documents.Pdf, MimeTypes.Pdf },
        { Documents.Doc, MimeTypes.Doc },
        { Documents.Docx, MimeTypes.Docx },
        { Documents.Rtf, MimeTypes.Rtf },
        
        // Images
        { Images.Png, MimeTypes.Png },
        { Images.Jpg, MimeTypes.Jpeg },
        { Images.Jpeg, MimeTypes.Jpeg },
        { Images.Gif, MimeTypes.Gif },
        { Images.Bmp, MimeTypes.Bmp },
        { Images.Tiff, MimeTypes.Tiff },
        { Images.Tif, MimeTypes.Tiff },
        { Images.Svg, MimeTypes.Svg },
        
        // Text
        { Text.PlainText, MimeTypes.PlainText },
        { Text.Html, MimeTypes.Html },
        { Text.Htm, MimeTypes.Html },
        { Text.Markdown, MimeTypes.Markdown },
        { Text.MarkdownFull, MimeTypes.Markdown },
        { Text.Xml, MimeTypes.Xml },
        { Text.Json, MimeTypes.Json },
        { Text.Csv, MimeTypes.Csv },
        
        // Spreadsheets
        { Spreadsheets.Xls, MimeTypes.Xls },
        { Spreadsheets.Xlsx, MimeTypes.Xlsx },
        
        // Presentations
        { Presentations.Ppt, MimeTypes.Ppt },
        { Presentations.Pptx, MimeTypes.Pptx },
        
        // Audio
        { Audio.Mp3, MimeTypes.Mp3 },
        { Audio.Wav, MimeTypes.Wav },
        { Audio.Flac, MimeTypes.Flac },
        { Audio.Ogg, MimeTypes.Ogg },
        { Audio.Aac, MimeTypes.Aac },
        
        // Archives
        { Archives.Zip, MimeTypes.Zip },
        { Archives.Rar, MimeTypes.Rar },
        { Archives.SevenZ, MimeTypes.SevenZ },
        { Archives.Tar, MimeTypes.Tar },
        { Archives.Gz, MimeTypes.Gzip }
    };
    
    #endregion
    
    #region Supported File Collections
    
    /// <summary>
    /// Gets all supported file extensions across all categories.
    /// </summary>
    public static readonly string[] AllSupportedExtensions = Documents.AllExtensions
        .Concat(Images.AllExtensions)
        .Concat(Text.AllExtensions)
        .Concat(Spreadsheets.AllExtensions)
        .Concat(Presentations.AllExtensions)
        .Concat(Audio.AllExtensions)
        .Concat(Archives.AllExtensions)
        .ToArray();
    
    /// <summary>
    /// Gets all supported MIME types.
    /// </summary>
    public static readonly string[] AllSupportedMimeTypes = ExtensionToMimeTypeMap.Values.Distinct().ToArray();
    
    /// <summary>
    /// Common file extensions used in document processing operations.
    /// </summary>
    public static readonly string[] DocumentProcessingExtensions = Documents.AllExtensions
        .Concat(Images.AllExtensions)
        .Concat(Text.AllExtensions)
        .ToArray();
    
    /// <summary>
    /// MIME types commonly used in document processing operations.
    /// </summary>
    public static readonly string[] DocumentProcessingMimeTypes = new[]
    {
        MimeTypes.Pdf, MimeTypes.Png, MimeTypes.Jpeg, MimeTypes.PlainText,
        MimeTypes.Html, MimeTypes.Markdown, MimeTypes.Xml, MimeTypes.Json
    };
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Gets the MIME type for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>The corresponding MIME type, or application/octet-stream if not found.</returns>
    public static string GetMimeType(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return MimeTypes.OctetStream;
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return ExtensionToMimeTypeMap.TryGetValue(extension, out var mimeType) 
            ? mimeType 
            : MimeTypes.OctetStream;
    }
    
    /// <summary>
    /// Validates if a file extension is supported.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>True if the extension is supported, false otherwise.</returns>
    public static bool IsExtensionSupported(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return ExtensionToMimeTypeMap.ContainsKey(extension);
    }
    
    /// <summary>
    /// Validates if a file extension is in the specified category.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <param name="allowedExtensions">Array of allowed extensions.</param>
    /// <returns>True if the extension is in the allowed list, false otherwise.</returns>
    public static bool IsExtensionInCategory(string filePath, string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(filePath) || allowedExtensions == null || allowedExtensions.Length == 0)
            return false;
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Validates if a MIME type matches the file's actual MIME type.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <param name="expectedMimeType">The expected MIME type.</param>
    /// <returns>True if the MIME types match, false otherwise.</returns>
    public static bool ValidateMimeType(string filePath, string expectedMimeType)
    {
        var actualMimeType = GetMimeType(filePath);
        return string.Equals(actualMimeType, expectedMimeType, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Validates if a file's MIME type is in the allowed list.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <param name="allowedMimeTypes">Array of allowed MIME types.</param>
    /// <returns>True if the MIME type is allowed, false otherwise.</returns>
    public static bool IsMimeTypeAllowed(string filePath, string[] allowedMimeTypes)
    {
        if (allowedMimeTypes == null || allowedMimeTypes.Length == 0)
            return false;
        
        var mimeType = GetMimeType(filePath);
        return allowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Gets the file type category for a given file path.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>The file type category as a string.</returns>
    public static string GetFileTypeCategory(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "Unknown";
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        if (Documents.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Document";
        
        if (Images.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Image";
        
        if (Text.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Text";
        
        if (Spreadsheets.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Spreadsheet";
        
        if (Presentations.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Presentation";
        
        if (Audio.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Audio";
        
        if (Archives.AllExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return "Archive";
        
        return "Unknown";
    }
    
    /// <summary>
    /// Checks if a file is a document type (PDF, DOC, DOCX, etc.).
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>True if the file is a document type, false otherwise.</returns>
    public static bool IsDocument(string filePath) => IsExtensionInCategory(filePath, Documents.AllExtensions);
    
    /// <summary>
    /// Checks if a file is an image type.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>True if the file is an image type, false otherwise.</returns>
    public static bool IsImage(string filePath) => IsExtensionInCategory(filePath, Images.AllExtensions);
    
    /// <summary>
    /// Checks if a file is a text-based type.
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>True if the file is a text-based type, false otherwise.</returns>
    public static bool IsTextFile(string filePath) => IsExtensionInCategory(filePath, Text.AllExtensions);
    
    /// <summary>
    /// Checks if a file is suitable for document processing (documents, images, text files).
    /// </summary>
    /// <param name="filePath">The file path or name.</param>
    /// <returns>True if the file can be processed as a document, false otherwise.</returns>
    public static bool IsDocumentProcessingSupported(string filePath) => 
        IsExtensionInCategory(filePath, DocumentProcessingExtensions);
    
    #endregion
}