using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("models")]
[Produces("application/json")]
public class ModelsController : ControllerBase
{
    private readonly IModelsService _modelsService;

    public ModelsController(IModelsService modelsService)
    {
        _modelsService = modelsService;
    }
    
    [HttpGet]
    public async Task<IEnumerable<AesirModelInfo>> GetAsync()
    {
        return await _modelsService.GetModelsAsync();
    }
}