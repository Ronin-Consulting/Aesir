namespace Aesir.Api.Server.Services;

/// <summary>
/// Represents a temporary file that will be automatically cleaned up when disposed.
/// </summary>
public sealed class TempFileHandle : IDisposable
{
    private readonly string _filePath;
    private bool _disposed = false;

    /// <summary>
    /// Gets the path to the temporary file.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Initializes a new instance of TempFileHandle.
    /// </summary>
    /// <param name="filePath">The path to the temporary file.</param>
    public TempFileHandle(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Creates a temporary file with the specified extension and content.
    /// </summary>
    /// <param name="extension">The file extension (including the dot).</param>
    /// <param name="content">The file content to write.</param>
    /// <returns>A TempFileHandle that will clean up the file when disposed.</returns>
    public static async Task<TempFileHandle> CreateAsync(string extension, byte[] content)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        await File.WriteAllBytesAsync(tempFilePath, content);
        return new TempFileHandle(tempFilePath);
    }

    /// <summary>
    /// Disposes the temporary file handle and cleans up the associated file.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method that handles the actual cleanup.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch
            {
                // Ignore cleanup errors - this is best effort
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer to ensure cleanup even if Dispose is not called.
    /// </summary>
    ~TempFileHandle()
    {
        Dispose(false);
    }
}