namespace Aesir.Infrastructure.Services;

/// <summary>
/// Represents a model location descriptor that provides identifiers for
/// inference engine and model configurations. This class encapsulates the
/// association between a specific inference engine instance and a model
/// utilized for tasks such as embedding generation or vision processing.
/// </summary>
public class ModelLocationDescriptor
{
    public string InterfaceEngineId { get; }

    public string ModelId { get; }
    
    public ModelLocationDescriptor(Guid interfaceEngineId, string modelId)
    {
        InterfaceEngineId = interfaceEngineId.ToString();
        ModelId = modelId;
    }

    // Backwards compatibility constructor
    public ModelLocationDescriptor(string interfaceEngineId, string modelId)
        : this(Guid.Parse(interfaceEngineId), modelId)
    {
    }
}