using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("documents")]
[Produces("application/json")]
public class DocumentCollectionController : ControllerBase
{
    private readonly ILogger<DocumentCollectionController> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPdfDataLoader _pdfDataLoader;
    
    public DocumentCollectionController(
        ILogger<DocumentCollectionController> logger,
        IFileStorageService fileStorageService, 
        IPdfDataLoader pdfDataLoader)
    {
        _logger = logger;
        _fileStorageService = fileStorageService;
        _pdfDataLoader = pdfDataLoader;
    }
    
    [HttpGet("file/{filename}/content")]
    public async Task<IActionResult> GetFileContentAsync([FromRoute]string filename)
    {
        var result = await _fileStorageService.GetFileContentAsync(filename);
        
        if(result == null || !System.IO.File.Exists(result.Value.FilePath))
            return NotFound();

        var fileStream = new FileStream(result.Value.FilePath, FileMode.Open, FileAccess.Read);
        var contentType = result.Value.FileInfo.MimeType;

        return new FileStreamResult(fileStream, contentType)
        {
            FileDownloadName = filename
        };
    }
    
    [HttpGet("load/test/data")]
    public async Task<IActionResult> LoadTestDataAsync()
    {
        const string filePath = "Assets/MissionPlan-OU812.pdf";
        
        await _pdfDataLoader.LoadPdfAsync(filePath, 2, 100, CancellationToken.None);

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var filename = Path.GetFileName(filePath);
        var mimeType = filename.GetContentType();
        
        await _fileStorageService.UpsertFileAsync(filename, mimeType, bytes);
            
        return Ok();
    }
}