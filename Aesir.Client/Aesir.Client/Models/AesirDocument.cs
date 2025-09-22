using System.IO;
using Aesir.Common.Models;

namespace Aesir.Client.Models;

/// <summary>
/// Represents a document in the client application, extending the base document model
/// with display-specific properties and formatting capabilities.
/// </summary>
/// <remarks>
/// This class provides formatted display properties for file information such as
/// file name, size, and timestamps. It inherits core document properties from
/// <see cref="AesirDocumentBase"/> and adds client-specific presentation logic.
/// </remarks>
public class AesirDocument:AesirDocumentBase
{
    /// <summary>
    /// Gets the display name of the file, extracted from the full file path.
    /// </summary>
    /// <value>The file name without the directory path.</value>
    public string FileNameDisplay => Path.GetFileName(FileName);

    /// <summary>
    /// Gets the file name only, extracted from the full file path.
    /// </summary>
    /// <value>The file name without the directory path.</value>
    /// <remarks>This property is functionally identical to <see cref="FileNameDisplay"/>.</remarks>
    public string FileNameOnly => Path.GetFileName(FileName);

    /// <summary>
    /// Gets the formatted file size as a human-readable string.
    /// </summary>
    /// <value>A string representation of the file size with appropriate units (B, KB, MB, GB, TB).</value>
    public string FileSizeDisplay
    {
        get
        {
            return FormatFileSize(FileSize);
        }
    }

    /// <summary>
    /// Gets the formatted creation date and time for display purposes.
    /// </summary>
    /// <value>A string representation of the creation date in general date/time format.</value>
    public string CreatedAtDisplay=> CreatedAt.ToString("g");

    /// <summary>
    /// Gets the formatted last updated date and time for display purposes.
    /// </summary>
    /// <value>A string representation of the last updated date in general date/time format.</value>
    public string UpdatedAtDisplay=> UpdatedAt.ToString("g");

    /// <summary>
    /// Formats a file size in bytes to a human-readable string with appropriate units.
    /// </summary>
    /// <param name="bytes">The file size in bytes to format.</param>
    /// <returns>A formatted string representing the file size with units (B, KB, MB, GB, TB).</returns>
    /// <remarks>
    /// The method automatically selects the most appropriate unit based on the file size,
    /// using 1024 as the conversion factor between units.
    /// </remarks>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.#} {sizes[order]}";
    }

}
