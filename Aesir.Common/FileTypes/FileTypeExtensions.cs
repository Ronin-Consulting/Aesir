namespace Aesir.Common.FileTypes;

/// <summary>
/// Extension methods for file type operations using the centralized FileTypeManager.
/// </summary>
public static class FileTypeExtensions
{
    /// <summary>
    /// Gets the MIME content type for a file based on its file path extension.
    /// </summary>
    /// <param name="filePath">The file path to determine the content type for.</param>
    /// <returns>The MIME content type or "application/octet-stream" if the content type cannot be determined.</returns>
    public static string GetContentType(this string filePath) => FileTypeManager.GetMimeType(filePath);
    
    /// <summary>
    /// Validates whether the expected content type matches the actual content type of the specified file path.
    /// </summary>
    /// <param name="filePath">The file path whose content type needs to be validated.</param>
    /// <param name="expectedContentType">The expected content type for validation.</param>
    /// <param name="actualContentType">The actual content type determined from the file path.</param>
    /// <returns>True if the file's actual content type matches the expected content type; otherwise, false.</returns>
    public static bool ValidFileContentType(this string filePath, string expectedContentType, out string actualContentType)
    {
        actualContentType = FileTypeManager.GetMimeType(filePath);
        return FileTypeManager.ValidateMimeType(filePath, expectedContentType);
    }
    
    /// <summary>
    /// Validates whether the file's content type is among the allowed content types.
    /// </summary>
    /// <param name="filePath">The file path whose content type needs to be validated.</param>
    /// <param name="actualContentType">The actual content type determined from the file path.</param>
    /// <param name="allowedContentTypes">Array of allowed content types.</param>
    /// <returns>True if the file's content type is in the allowed list; otherwise, false.</returns>
    public static bool ValidFileContentType(this string filePath, out string actualContentType, params string[] allowedContentTypes)
    {
        actualContentType = FileTypeManager.GetMimeType(filePath);
        return FileTypeManager.IsMimeTypeAllowed(filePath, allowedContentTypes);
    }
    
    /// <summary>
    /// Checks if the file extension is supported by the system.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the extension is supported, false otherwise.</returns>
    public static bool IsSupportedFileType(this string filePath) => FileTypeManager.IsExtensionSupported(filePath);
    
    /// <summary>
    /// Gets the file type category (Document, Image, Text, etc.).
    /// </summary>
    /// <param name="filePath">The file path to categorize.</param>
    /// <returns>The file type category as a string.</returns>
    public static string GetFileTypeCategory(this string filePath) => FileTypeManager.GetFileTypeCategory(filePath);
    
    /// <summary>
    /// Checks if the file is a document type (PDF, DOC, DOCX, etc.).
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a document type, false otherwise.</returns>
    public static bool IsDocument(this string filePath) => FileTypeManager.IsDocument(filePath);
    
    /// <summary>
    /// Checks if the file is an image type.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is an image type, false otherwise.</returns>
    public static bool IsImage(this string filePath) => FileTypeManager.IsImage(filePath);
    
    /// <summary>
    /// Checks if the file is a text-based type.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a text-based type, false otherwise.</returns>
    public static bool IsTextFile(this string filePath) => FileTypeManager.IsTextFile(filePath);
    
    /// <summary>
    /// Checks if the file is suitable for document processing.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file can be processed as a document, false otherwise.</returns>
    public static bool IsDocumentProcessingSupported(this string filePath) => FileTypeManager.IsDocumentProcessingSupported(filePath);
}