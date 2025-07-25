namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Configuration class for specifying settings and parameters for the vision AI model,
/// such as the model identifier used by the VisionService.
/// </summary>
public class VisionModelConfig
{
    /// <summary>
    /// Represents the identifier of the vision model configured for processing images and extracting textual information.
    /// </summary>
    public required string ModelId { get; set; }
}