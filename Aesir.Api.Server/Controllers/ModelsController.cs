using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

/// <summary>
/// Controller responsible for handling requests related to AI models.
/// </summary>
[ApiController]
[Route("models")]
[Produces("application/json")]
public class ModelsController(IModelsService modelsService) : ControllerBase
{
    /// <summary>
    /// Retrieves information about available AI models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns a collection of AI model information.</returns>
    [HttpGet]
    public async Task<IEnumerable<AesirModelInfo>> GetAsync()
    {
        return await modelsService.GetModelsAsync();
    }
}