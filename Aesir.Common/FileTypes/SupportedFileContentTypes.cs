namespace Aesir.Common.FileTypes;

/// <summary>
/// Provides constants for supported file content types.
/// This class serves as a centralized location for MIME type constants used throughout the Aesir solution.
/// Migrated from Aesir.Api.Server to provide a single source of truth for all projects.
/// </summary>
public static class SupportedFileContentTypes
{
    #region Document Content Types
    /// <summary>
    /// Represents the MIME type for PDF files.
    /// </summary>
    public const string PdfContentType = FileTypeManager.MimeTypes.Pdf;
    
    /// <summary>
    /// Represents the MIME type for DOC files.
    /// </summary>
    public const string DocContentType = FileTypeManager.MimeTypes.Doc;
    
    /// <summary>
    /// Represents the MIME type for DOCX files.
    /// </summary>
    public const string DocxContentType = FileTypeManager.MimeTypes.Docx;
    
    /// <summary>
    /// Represents the MIME type for RTF files.
    /// </summary>
    public const string RtfContentType = FileTypeManager.MimeTypes.Rtf;
    #endregion
    
    #region Image Content Types
    /// <summary>
    /// The MIME type for PNG image files.
    /// </summary>
    public const string PngContentType = FileTypeManager.MimeTypes.Png;
    
    /// <summary>
    /// The MIME type for JPEG image files.
    /// </summary>
    public const string JpegContentType = FileTypeManager.MimeTypes.Jpeg;
    
    /// <summary>
    /// The MIME type for TIFF image files.
    /// </summary>
    public const string TiffContentType = FileTypeManager.MimeTypes.Tiff;
    
    /// <summary>
    /// The MIME type for BMP image files.
    /// </summary>
    public const string BmpContentType = FileTypeManager.MimeTypes.Bmp;
    
    /// <summary>
    /// The MIME type for GIF image files.
    /// </summary>
    public const string GifContentType = FileTypeManager.MimeTypes.Gif;
    
    /// <summary>
    /// The MIME type for SVG image files.
    /// </summary>
    public const string SvgContentType = FileTypeManager.MimeTypes.Svg;
    #endregion
    
    #region Text Based File Content Types
    /// <summary>
    /// The MIME type for plain text files.
    /// </summary>
    public const string PlainTextContentType = FileTypeManager.MimeTypes.PlainText;
    
    /// <summary>
    /// The MIME type for HTML files.
    /// </summary>
    public const string HtmlContentType = FileTypeManager.MimeTypes.Html;
    
    /// <summary>
    /// The MIME type for Markdown files.
    /// </summary>
    public const string MarkdownContentType = FileTypeManager.MimeTypes.Markdown;
    
    /// <summary>
    /// The MIME type for XML files.
    /// </summary>
    public const string XmlContentType = FileTypeManager.MimeTypes.Xml;
    
    /// <summary>
    /// The MIME type for JSON files.
    /// </summary>
    public const string JsonContentType = FileTypeManager.MimeTypes.Json;
    
    /// <summary>
    /// The MIME type for CSV files.
    /// </summary>
    public const string CsvContentType = FileTypeManager.MimeTypes.Csv;
    #endregion
    
    #region Office Document Content Types
    /// <summary>
    /// The MIME type for Excel XLS files.
    /// </summary>
    public const string XlsContentType = FileTypeManager.MimeTypes.Xls;
    
    /// <summary>
    /// The MIME type for Excel XLSX files.
    /// </summary>
    public const string XlsxContentType = FileTypeManager.MimeTypes.Xlsx;
    
    /// <summary>
    /// The MIME type for PowerPoint PPT files.
    /// </summary>
    public const string PptContentType = FileTypeManager.MimeTypes.Ppt;
    
    /// <summary>
    /// The MIME type for PowerPoint PPTX files.
    /// </summary>
    public const string PptxContentType = FileTypeManager.MimeTypes.Pptx;
    #endregion
    
    #region Audio Content Types
    /// <summary>
    /// The MIME type for MP3 audio files.
    /// </summary>
    public const string Mp3ContentType = FileTypeManager.MimeTypes.Mp3;
    
    /// <summary>
    /// The MIME type for WAV audio files.
    /// </summary>
    public const string WavContentType = FileTypeManager.MimeTypes.Wav;
    
    /// <summary>
    /// The MIME type for FLAC audio files.
    /// </summary>
    public const string FlacContentType = FileTypeManager.MimeTypes.Flac;
    
    /// <summary>
    /// The MIME type for OGG audio files.
    /// </summary>
    public const string OggContentType = FileTypeManager.MimeTypes.Ogg;
    #endregion
    
    #region Archive Content Types
    /// <summary>
    /// The MIME type for ZIP archive files.
    /// </summary>
    public const string ZipContentType = FileTypeManager.MimeTypes.Zip;
    #endregion
    
    #region Default Content Type
    /// <summary>
    /// The default MIME type for unknown or binary files.
    /// </summary>
    public const string OctetStreamContentType = FileTypeManager.MimeTypes.OctetStream;
    #endregion
}