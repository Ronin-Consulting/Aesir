using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Modules.Inference.Controllers;

/// <summary>
/// Controller responsible for handling requests related to AI models.
/// </summary>
[ApiController]
[Route("models")]
[Produces("application/json")]
public class ModelsController(IServiceProvider serviceProvider, IConfigurationService configurationService) : ControllerBase
{
    /// <summary>
    /// Retrieves information about available AI models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns a collection of AI model information.</returns>
    [HttpGet("{inferenceEngineId:guid}/{category}")]
    public async Task<IEnumerable<AesirModelInfo>> GetModels([FromRoute] Guid inferenceEngineId, [FromRoute]ModelCategory? category)
    {
        // Resolve the correct ModelsService based on the inference engine
        var modelsService = serviceProvider.GetKeyedService<IModelsService>(inferenceEngineId.ToString());
        if (modelsService == null)
        {
            throw new InvalidOperationException($"No models service found for inference engine ID: {inferenceEngineId}");
        }
            
        return await modelsService.GetModelsAsync(category);
    }
}