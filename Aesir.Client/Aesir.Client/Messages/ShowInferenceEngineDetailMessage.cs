using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of an inference engine in the system.
/// </summary>
public class ShowInferenceEngineDetailMessage(AesirInferenceEngineBase? inferenceEngine)
{
    /// <summary>
    /// Represents an inference engine entity, encapsulated within the <see cref="ShowInferenceEngineDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirInferenceEngineBase"/> which provides core inference
    /// engine information such as ID, name, type, description, and configuration.
    /// </summary>
    public AesirInferenceEngineBase InferenceEngine { get; set; } = inferenceEngine ?? new AesirInferenceEngineBase();
}