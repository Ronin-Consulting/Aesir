using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

/// <summary>
/// A FileStreamResult that properly disposes of temporary files when the stream is disposed.
/// </summary>
public class TempFileStreamResult : FileStreamResult, IDisposable
{
    private readonly TempFileHandle _tempFileHandle;

    /// <summary>
    /// Initializes a new instance of TempFileStreamResult.
    /// </summary>
    /// <param name="fileStream">The file stream to return.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="tempFileHandle">The temporary file handle to dispose when done.</param>
    public TempFileStreamResult(FileStream fileStream, string contentType, TempFileHandle tempFileHandle)
        : base(fileStream, contentType)
    {
        _tempFileHandle = tempFileHandle;
    }


    public void Dispose()
    {
        _tempFileHandle.Dispose();
    }
}