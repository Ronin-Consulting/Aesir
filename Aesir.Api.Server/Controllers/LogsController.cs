using Aesir.Api.Server.Services;
using Aesir.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("logs")]
[Produces("application/json")]
public class LogsController(
    ILogger<LogsController> logger,
    IKernelLogService kernelLogService )
: ControllerBase
{
    [HttpGet("kernel")]
    public async Task<IEnumerable<AesirKernelLogBase>> GetKernelLogs([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        return await kernelLogService.GetLogsAsync(from, to);
    }

}