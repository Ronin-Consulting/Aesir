using Microsoft.SemanticKernel;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Represents a service interface for handling kernel plugin operations.
/// </summary>
public interface IKernelPluginService
{
    /// <summary>
    /// Asynchronously retrieves a kernel plugin using the provided arguments.
    /// </summary>
    /// <param name="kernelPluginArguments">
    /// A dictionary containing the arguments required to configure the kernel plugin. The dictionary must include at least a "PluginName" key with a string value.
    /// </param>
    /// <returns>
    /// A <see cref="KernelPlugin"/> instance created based on the provided arguments.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the required "PluginName" key is not present in the <paramref name="kernelPluginArguments"/> dictionary.
    /// </exception>
    Task<KernelPlugin> GetKernelPluginAsync(IDictionary<string, object>? kernelPluginArguments);
}
