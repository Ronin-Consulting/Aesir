using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("document/collections")]
[Produces("application/json")]
public class DocumentCollectionController : ControllerBase
{
    private readonly ILogger<DocumentCollectionController> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPdfDataLoaderService _pdfDataLoaderService;
    
    public DocumentCollectionController(
        ILogger<DocumentCollectionController> logger,
        IFileStorageService fileStorageService, 
        IPdfDataLoaderService pdfDataLoaderService)
    {
        _logger = logger;
        _fileStorageService = fileStorageService;
        _pdfDataLoaderService = pdfDataLoaderService;
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
            FileDownloadName = filename,
            EnableRangeProcessing = true
        };
    }
    
    [HttpGet("load/test/data")]
    public async Task<IActionResult> LoadTestDataAsync()
    {
        const string filePath = "Assets/MissionPlan-OU812.pdf";
        
        await _pdfDataLoaderService.LoadPdfAsync(filePath, 2, 100, CancellationToken.None);

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var filename = Path.GetFileName(filePath);
        var mimeType = filename.GetContentType();
        
        await _fileStorageService.UpsertFileAsync(filename, mimeType, bytes);
            
        return Ok();
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104857600)] // 100MB
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<IActionResult> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > 104857600) // 100MB
            return BadRequest("File size exceeds 100MB limit.");

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (fileExtension != ".pdf")
            return BadRequest("Only PDF files are allowed.");

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            var mimeType = file.ContentType ?? "application/pdf";
            
            await _fileStorageService.UpsertFileAsync(file.FileName, mimeType, fileContent);

            return Ok(new { message = "File uploaded successfully", fileName = file.FileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "An error occurred while uploading the file.");
        }
    }
}