using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

/// <summary>
/// Controller responsible for handling requests related to AI models.
/// </summary>
[ApiController]
[Route("models")]
[Produces("application/json")]
public class ModelsController(IModelsService modelsService, IConfigurationService configurationService) : ControllerBase
{
    /// <summary>
    /// Retrieves information about available AI models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns a collection of AI model information.</returns>
    [HttpGet("{inferenceEngineId:guid}/{category}")]
    public async Task<IEnumerable<AesirModelInfo>> GetModels([FromRoute] Guid inferenceEngineId, [FromRoute]ModelCategory? category)
    {
        // TODO lookup inference engine and check type
        // TODO lookup proper modelsservice by name
        // TODO ask it for models

        var inferenceEngine= await configurationService.GetInferenceEngineAsync(inferenceEngineId);
        
        //inferenceEngine.Type
        //inferenceEngine.Name
            
        return await modelsService.GetModelsAsync(category);
    }
}