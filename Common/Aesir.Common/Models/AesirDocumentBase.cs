namespace Aesir.Common.Models;

public abstract class AesirDocumentBase
{
    /*
     * This currently mirrors AesirFileInfo in the server project.
     */
    
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MIME type of the file.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the file was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the file was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
}
