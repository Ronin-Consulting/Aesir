using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("models")]
[Produces("application/json")]
public class ModelsController(IModelsService modelsService) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AesirModelInfo>> GetAsync()
    {
        return await modelsService.GetModelsAsync();
    }
}