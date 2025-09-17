namespace Aesir.Api.Server.Services;

/// <summary>
/// Represents a model location descriptor that provides identifiers for
/// inference engine and model configurations. This class encapsulates the
/// association between a specific inference engine instance and a model
/// utilized for tasks such as embedding generation or vision processing.
/// </summary>
public class ModelLocationDescriptor(Guid interfaceEngineId, string modelId)
{
    public string InterfaceEngineId { get; } = interfaceEngineId.ToString();

    public string ModelId { get; } = modelId;
}