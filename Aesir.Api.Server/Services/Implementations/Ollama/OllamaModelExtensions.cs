using Ollama;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

public static class OllamaModelExtensions
{
    public static string GetModelId(this Model model)
    {
        return (model.AdditionalProperties.TryGetValue("name", out var modelName) ? modelName.ToString() : "no-id") ?? throw new InvalidOperationException();
    }
}