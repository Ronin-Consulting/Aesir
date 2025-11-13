using System.Text.Json;
using Aesir.Common.Models;
using Aesir.Modules.Logging.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Provides Semantic Kernel filter implementations for logging function invocations,
/// prompt rendering, and automatic function invocations to the kernel log system.
/// Acts as middleware to capture all kernel operations for auditing and debugging.
/// </summary>
/// <param name="logger">The logger instance for capturing log messages related to kernel activities.</param>
/// <param name="kernelLogService">The kernel log service for persisting structured kernel operation logs.</param>
public class KernelLoggingFilterService(ILogger<KernelLoggingFilterService> logger, IKernelLogService kernelLogService)
    : IFunctionInvocationFilter, IPromptRenderFilter, IAutoFunctionInvocationFilter
{
    /// <summary>
    /// Provides configuration options to control the behavior of the System.Text.Json.JsonSerializer.
    /// </summary>
    /// <remarks>
    /// This property is used to customize JSON serialization and deserialization behavior globally or locally within specific contexts.
    /// It can be utilized to configure the formatting, serialization rules, or handling of specific types during JSON operations.
    /// </remarks>
    private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Handles function invocation events by logging relevant details such as function name, arguments, and results.
    /// </summary>
    /// <param name="context">
    /// The context of the function invocation, including details about the function, arguments, and results.
    /// </param>
    /// <param name="next">
    /// A delegate to invoke the next step in the function invocation pipeline.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation of the function invocation lifecycle.
    /// </returns>
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        logger.LogDebug("OnFunctionInvocationAsync --> Function Invocation: {FunctionName}", context.Function.Name);

        var details = GetFunctionContext(context);
        await kernelLogService.LogAsync(KernelLogLevel.Info, $"Function Invocation: {context.Function.Name}", details);
        
        await next(context);

        foreach (var argument in context.Arguments.Names)
        {
            logger.LogDebug("OnFunctionInvocationAsync --> Function {FunctionName} --> Argument: {Argument} --> Value: {Value}", context.Function.Name, argument, JsonSerializer.Serialize(context.Arguments[argument], JsonSerializerOptions));
        }

        logger.LogDebug("OnFunctionInvocationAsync --> Function {FunctionName} --> Result: {Result}", context.Function.Name, JsonSerializer.Serialize(context.Result.GetValue<object?>(), JsonSerializerOptions));
    }

    /// <summary>
    /// Handles the rendering of a prompt for a specific function and logs the details of the rendering process.
    /// </summary>
    /// <param name="context">The context of the prompt rendering, including details about the function and the rendered prompt.</param>
    /// <param name="next">A delegate to invoke the next step in the prompt rendering pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        logger.LogDebug("OnPromptRenderAsync --> Prompt Render for: {FunctionName}", context.Function.Name);
        
        var details = GetPromptRenderContext(context);
        await kernelLogService.LogAsync(KernelLogLevel.Info, $"Prompt Render: {context.Function.Name}", details);

        await next(context);
        
        logger.LogDebug("OnPromptRenderAsync --> Prompt Render for {FunctionName} --> Prompt: {Prompt}", context.Function.Name, context.RenderedPrompt);
    }

    /// <summary>
    /// Logs details about the invocation of an auto function, including its arguments, request sequence index, tool call ID, chat history, and result.
    /// </summary>
    /// <param name="context">The context of the auto function invocation containing details such as function metadata, arguments, and execution state.</param>
    /// <param name="next">A delegate to invoke the next filter or middleware in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        logger.LogDebug("OnAutoFunctionInvocationAsync --> Function Invocation: {FunctionName}", context.Function.Name);

        var details = GetAutoFunctionContext(context);
        await kernelLogService.LogAsync(KernelLogLevel.Info, $"Auto Function Invocation: {context.Function.Name}", details);

        if (context.Arguments != null)
        {
            foreach (var argument in context.Arguments.Names)
            {
                logger.LogDebug("OnAutoFunctionInvocationAsync --> Function {FunctionName} --> Argument: {Argument} --> Value: {Value}", context.Function.Name, argument, JsonSerializer.Serialize(context.Arguments[argument], JsonSerializerOptions));
            }    
        }
        
        await next(context);
        
        logger.LogDebug("OnAutoFunctionInvocationAsync --> Function {FunctionName} --> Request Sequence Index: {RequestSequenceIndex}", context.Function.Name, context.RequestSequenceIndex);
        
        logger.LogDebug("OnAutoFunctionInvocationAsync --> Function {FunctionName} --> Tool Call Id: {ToolCallId}", context.Function.Name, context.ToolCallId);
        
        logger.LogDebug("OnAutoFunctionInvocationAsync --> Function {FunctionName} --> Chat History: {ChatHistory}", context.Function.Name, JsonSerializer.Serialize(context.ChatHistory, JsonSerializerOptions));
        
        logger.LogDebug("OnAutoFunctionInvocationAsync --> Function {FunctionName} --> Result: {Result}", context.Function.Name, JsonSerializer.Serialize(context.Result.GetValue<object?>(), JsonSerializerOptions));
    }

    private KernelLogDetails GetKernelChatDetails(Kernel kernel)
    {
        var details = new KernelLogDetails()
        {
            ChatSessionId = kernel.Data.ContainsKey("ChatSessionId")
                ? (Guid)(kernel.Data["ChatSessionId"])
                : null,
            ConversationId = kernel.Data.ContainsKey("ConversationId")
                ? Guid.Parse(kernel.Data["ConversationId"].ToString())
                : null,
        };
        return details;
    }

    private KernelLogDetails GetFunctionContext(FunctionInvocationContext context)
    {
        var details = GetKernelChatDetails(context.Kernel);

        details.Type = KernelLogType.FunctionInvocation;
        details.Arguments = context.Arguments?.Select(x =>
                new KeyValuePair<string, string>(x.Key, JsonSerializer.Serialize(x.Value, JsonSerializerOptions)))
            .ToList();
        details.FunctionName = context.Function.Name;
        details.FunctionDescription = context.Function.Description;
        details.PluginName = context.Function.PluginName;
        details.UnderlyingMethod = context.Function.UnderlyingMethod?.Name;
        
        return details;
    }

    private KernelLogDetails GetAutoFunctionContext(AutoFunctionInvocationContext context)
    {
        var details = GetKernelChatDetails(context.Kernel);
        
        details.Type = KernelLogType.AutoFunctionInvocation;
        details.Arguments = context.Arguments?.Select(x =>
                new KeyValuePair<string, string>(x.Key, JsonSerializer.Serialize(x.Value, JsonSerializerOptions)))
            .ToList();
        details.FunctionName = context.Function.Name;
        details.FunctionDescription = context.Function.Description;
        details.PluginName = context.Function.PluginName;
        details.UnderlyingMethod = context.Function.UnderlyingMethod?.Name;
    
        return details;
    }

    private KernelLogDetails GetPromptRenderContext(PromptRenderContext context)
    {
        var details = GetKernelChatDetails(context.Kernel);
        
        details.Type = KernelLogType.AutoFunctionInvocation;
        details.Arguments = context.Arguments?.Select(x =>
                new KeyValuePair<string, string>(x.Key, JsonSerializer.Serialize(x.Value, JsonSerializerOptions)))
            .ToList();
        details.FunctionName = context.Function.Name;
        details.FunctionDescription = context.Function.Description;
        details.PluginName = context.Function.PluginName;
        details.UnderlyingMethod = context.Function.UnderlyingMethod?.Name;

        return details;
    }
}