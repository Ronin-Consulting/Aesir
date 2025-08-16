namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides constants for supported file content types.
/// </summary>
public static class SupportedFileContentTypes
{
    #region Document Content Types
    /// <summary>
    /// Represents the MIME type for PDF files.
    /// </summary>
    public static readonly string PdfContentType = "application/pdf";
    #endregion
    
    #region Image Content Types
    /// <summary>
    /// The MIME type for PNG image files.
    /// </summary>
    public static readonly string PngContentType = "image/png";
    
    public static readonly string JpegContentType = "image/jpeg";
    
    public static readonly string TiffContentType = "image/tiff";
    
    public static readonly string BmpContentType = "image/bmp";
    #endregion
    
    #region Text Based File Content Types
    public static readonly string PlainTextContentType = "text/plain";
    public static readonly string HtmlContentType = "text/html";
    public static readonly string MarkdownContentType = "text/markdown";
    public static readonly string RtfContentType = "text/rtf";
    public static readonly string XmlContentType = "text/xml";
    public static readonly string JsonContentType = "application/json";
    #endregion
}