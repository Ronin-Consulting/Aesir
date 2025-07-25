namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Represents the configuration settings required for the vision AI model.
/// </summary>
public class VisionModelConfig
{
    /// <summary>
    /// Gets or sets the identifier of the vision model used for processing.
    /// This property specifies the model identifier that the vision service utilizes
    /// for handling image processing tasks. It is required to correctly configure
    /// which vision model will be used during service operations.
    /// </summary>
    public required string ModelId { get; set; }
}