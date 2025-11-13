namespace Aesir.Modules.Documents.Models;

/// <summary>
/// Represents a request to load a text file with associated metadata and configurable processing parameters.
/// </summary>
public sealed class LoadTextFileRequest
{
    /// <summary>
    /// Gets or sets the local file path of the text file.
    /// </summary>
    public string? TextFileLocalPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the text file.
    /// </summary>
    public string? TextFileFileName { get; set; }

    /// <summary>
    /// Gets or sets a collection of key-value pairs representing metadata associated with the text file.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the size of the batch for processing text file content.
    /// Determines the number of content items to process in a single batch operation.
    /// </summary>
    public int BatchSize { get; set; } = Math.Max(Environment.ProcessorCount / 2 + 1, 10);
}
