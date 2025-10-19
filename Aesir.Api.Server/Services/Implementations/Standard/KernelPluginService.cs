using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

[Experimental("SKEXP0001")]
public class KernelPluginService(
    IDocumentCollectionService documentCollectionService,
    IConfigurationService configurationService,        
    VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> conversationDocumentVectorSearch,
    IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>? conversationDocumentHybridSearch,
    VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> vectorStoreRecordCollection)
    : IKernelPluginService
{
    /// <summary>
    /// Retrieves a kernel plugin by using the provided arguments to configure its creation and inclusion of plugin-specific functions.
    /// </summary>
    /// <param name="kernelPluginArguments">Dictionary containing arguments used to configure the kernel plugin.
    /// Must include a "PluginName" entry, and optionally may include an "McpTools" entry to configure specific tools for the plugin.</param>
    /// <returns>A <see cref="KernelPlugin"/> instance created from the provided arguments.</returns>
    /// <exception cref="ArgumentException">Thrown when the required "PluginName" argument is missing in the kernelPluginArguments dictionary.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a specified tool is not found in the supplied configuration or when related operations fail during plugin creation.</exception>
    public async Task<KernelPlugin> GetKernelPluginAsync(IDictionary<string, object>? kernelPluginArguments)
    {
        if (!kernelPluginArguments.TryGetValue("PluginName", out var pluginNameValue))
            throw new ArgumentException("Kernel plugin args must contain a PluginName");
        var pluginName = (string)pluginNameValue;
        
        var pluginFunctions = await documentCollectionService.GetKernelPluginFunctionsAsync(kernelPluginArguments);

        if (kernelPluginArguments.TryGetValue("McpTools", out var mcpTools))
        {
            var mcpServers = (await configurationService.GetMcpServersAsync()).ToList();
                
            var kernelFunctionLibrary = new KernelFunctionLibrary<Guid, AesirConversationDocumentTextData<Guid>>(
                conversationDocumentVectorSearch, conversationDocumentHybridSearch, vectorStoreRecordCollection
            );
            
            var mcpServerToolArgs = (BaseKernelPluginArgs.McpServerToolArg[])mcpTools;
            
            foreach (var mcpServerToolArg in mcpServerToolArgs)
            {
                var mcpServer = mcpServers.First(s => s.Name == mcpServerToolArg.McpServerName);

                if (mcpServer == null)
                    throw new ArgumentException($"Requested MCP Server {mcpServerToolArg.McpServerName} was not found");
                
                var function = await kernelFunctionLibrary.GetMcpServerToolFunctionAsync(mcpServer, mcpServerToolArg.ToolName);

                pluginFunctions.Add(function);
            }
        }

        return KernelPluginFactory.CreateFromFunctions(
            pluginName,
            pluginFunctions.ToArray()
        ); 
    }
}