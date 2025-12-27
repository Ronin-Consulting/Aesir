using Microsoft.AspNetCore.Components.Forms;

namespace Aesir.Client.Web.Modules.Chat.Models;

/// <summary>
/// Represents a file that is pending upload or currently being uploaded.
/// </summary>
public class PendingFileUpload
{
    /// <summary>
    /// Unique identifier for tracking this upload.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The browser file to be uploaded.
    /// </summary>
    public required IBrowserFile File { get; set; }

    /// <summary>
    /// The file name.
    /// </summary>
    public string FileName => File.Name;

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long FileSize => File.Size;

    /// <summary>
    /// The upload status.
    /// </summary>
    public UploadStatus Status { get; set; } = UploadStatus.Pending;

    /// <summary>
    /// Upload progress (0-100).
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Error message if the upload failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Cancellation token source for canceling the upload.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <summary>
    /// Object URL for client-side image thumbnail preview.
    /// Set via JavaScript for image file types.
    /// </summary>
    public string? ObjectUrl { get; set; }

    /// <summary>
    /// Indicates the file is being removed (for exit animation).
    /// </summary>
    public bool IsRemoving { get; set; }
}

/// <summary>
/// Status of a file upload.
/// </summary>
public enum UploadStatus
{
    /// <summary>
    /// File is pending upload.
    /// </summary>
    Pending,

    /// <summary>
    /// File is currently being uploaded.
    /// </summary>
    Uploading,

    /// <summary>
    /// File has been uploaded and is being processed/indexed in the background.
    /// </summary>
    Processing,

    /// <summary>
    /// File was uploaded successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// File upload failed.
    /// </summary>
    Failed,

    /// <summary>
    /// File upload was cancelled.
    /// </summary>
    Cancelled
}
