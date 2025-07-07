using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides logging functionality for inference operations including function invocations and prompt rendering.
/// </summary>
/// <param name="logger">The logger instance for recording inference operations.</param>
public class InferenceLoggingService(ILogger<InferenceLoggingService> logger)
    : IFunctionInvocationFilter, IPromptRenderFilter, IAutoFunctionInvocationFilter
{

    private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        WriteIndented = true
    };
    
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        logger.LogDebug("OnFunctionInvocationAsync --> Function Invocation: {FunctionName}", context.Function.Name);
        
        await next(context);

        foreach (var argument in context.Arguments.Names)
        {
            logger.LogDebug("OnFunctionInvocationAsync --> Function {FunctionName} --> Argument: {Argument} --> Value: {Value}", context.Function.Name, argument, JsonSerializer.Serialize(context.Arguments[argument], JsonSerializerOptions));
        }

        logger.LogDebug("OnFunctionInvocationAsync --> Function {FunctionName} --> Result: {Result}", context.Function.Name, JsonSerializer.Serialize(context.Result.GetValue<object?>(), JsonSerializerOptions));
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        logger.LogDebug("OnPromptRenderAsync --> Prompt Render for: {FunctionName}", context.Function.Name);
        
        await next(context);
        
        logger.LogDebug("OnPromptRenderAsync --> Prompt Render for {FunctionName} --> Prompt: {Prompt}", context.Function.Name, context.RenderedPrompt);
    }

    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        logger.LogDebug("OnAutoFunctionInvocationAsync --> Function Invocation: {FunctionName}", context.Function.Name);

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
}