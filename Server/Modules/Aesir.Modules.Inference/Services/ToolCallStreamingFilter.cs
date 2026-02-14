using System.Diagnostics;
using System.Text.Json;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Semantic Kernel Auto Function Invocation Filter that broadcasts tool calls to the streaming response
/// and collects them for observability logging.
/// Intercepts all tool/function calls made by SK and sends them to the active broadcaster scope
/// and inference log collector.
/// </summary>
public class ToolCallStreamingFilter : IAutoFunctionInvocationFilter
{
    private readonly ILogger<ToolCallStreamingFilter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Maximum length for result preview in the UI.
    /// </summary>
    private const int MaxResultLength = 500;

    /// <summary>
    /// Maximum number of auto-invocation iterations allowed per request.
    /// This prevents endless loops when models repeatedly call tools.
    /// Must match the limit specified in system prompt templates.
    /// </summary>
    private const int MaxAutoInvokeIterations = 8;

    /// <summary>
    /// Iteration count at which to start injecting warning messages to the LLM.
    /// Warning triggers at iterations 6 and 7 (2-3 calls before the hard limit).
    /// This gives the model a chance to synthesize a response before termination.
    /// </summary>
    private const int WarningIterationThreshold = 6;

    /// <summary>
    /// Warning message template appended to function results when approaching the iteration limit.
    /// Uses string.Format with {0}=current iteration, {1}=max iterations, {2}=remaining calls.
    /// </summary>
    private const string IterationWarningMessage =
        "\n\n[SYSTEM WARNING: You have executed {0} of {1} allowed tool calls. " +
        "You have {2} call(s) remaining. Please synthesize your findings and provide " +
        "your final answer now to avoid reaching the execution limit.]";

    public ToolCallStreamingFilter(ILogger<ToolCallStreamingFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        // Check iteration limit BEFORE executing to prevent endless loops
        if (context.RequestSequenceIndex >= MaxAutoInvokeIterations)
        {
            _logger.LogWarning(
                "Tool call iteration limit reached ({Current}/{Max}). Terminating auto-invocation for function: {FunctionName}",
                context.RequestSequenceIndex, MaxAutoInvokeIterations, context.Function.Name);
            context.Terminate = true;
            return;
        }

        _logger.LogDebug("ToolCallStreamingFilter invoked for function: {FunctionName} (Plugin: {PluginName}) [Iteration: {Iteration}]",
            context.Function.Name, context.Function.PluginName, context.RequestSequenceIndex);

        // Get the current broadcaster scope from Kernel.Data (for streaming to UI)
        var broadcasterScope = ToolCallBroadcaster.GetScope(context.Kernel);

        // Get the inference log collector from Kernel.Data (for observability persistence)
        var logCollector = InferenceLogCollector.GetFromKernel(context.Kernel);

        // If neither scope nor collector exists, just execute without UI tracking
        // but still inject iteration warnings so research agents get soft-landed
        if (broadcasterScope == null && logCollector == null)
        {
            _logger.LogDebug("No broadcaster scope or log collector in Kernel.Data - executing without tracking");
            await next(context);

            if (context.RequestSequenceIndex >= WarningIterationThreshold &&
                context.RequestSequenceIndex < MaxAutoInvokeIterations)
            {
                InjectIterationWarning(context);
            }

            return;
        }

        var toolCallId = context.ToolCallId ?? Guid.NewGuid().ToString();

        // Determine tool type from function name and plugin
        var toolType = DetermineToolType(context.Function.Name, context.Function.PluginName);

        // Create tool call info for start event
        var toolCallInfo = new AesirToolCallInfo
        {
            ToolCallId = toolCallId,
            FunctionName = context.Function.Name,
            PluginName = context.Function.PluginName,
            Description = context.Function.Description,
            ToolType = toolType,
            Arguments = ExtractArguments(context.Arguments),
            UnderlyingMethod = context.Function.UnderlyingMethod?.Name,
            Status = ToolCallStatus.Started,
            StartedAt = DateTimeOffset.UtcNow
        };

        // Start timer
        var stopwatch = Stopwatch.StartNew();

        // Broadcast start event to UI (if broadcaster exists)
        if (broadcasterScope != null)
        {
            _logger.LogDebug("Broadcasting tool call start: {FunctionName} ({ToolCallId})",
                context.Function.Name, toolCallId);
            await broadcasterScope.BroadcastStartAsync(toolCallInfo);
        }

        try
        {
            // Execute the actual function
            await next(context);

            // Inject warning message if approaching iteration limit (iterations 6-7)
            // This gives the LLM a chance to stop calling tools and synthesize a response
            if (context.RequestSequenceIndex >= WarningIterationThreshold &&
                context.RequestSequenceIndex < MaxAutoInvokeIterations)
            {
                InjectIterationWarning(context);
            }

            // Update tool call info with completion data
            stopwatch.Stop();
            toolCallInfo.Status = ToolCallStatus.Completed;
            toolCallInfo.CompletedAt = DateTimeOffset.UtcNow;
            toolCallInfo.Result = TruncateResult(context.Result?.GetValue<object?>());

            _logger.LogDebug("Tool call completed: {FunctionName} ({ToolCallId}) in {Duration}ms",
                context.Function.Name, toolCallId, stopwatch.ElapsedMilliseconds);

            // Broadcast completion to UI
            if (broadcasterScope != null)
            {
                await broadcasterScope.BroadcastCompletionAsync(toolCallInfo);
            }

            // Add to inference log collector for persistence
            if (logCollector != null)
            {
                logCollector.AddToolCall(toolCallInfo);
            }
        }
        catch (Exception ex)
        {
            // Update tool call info with failure data
            stopwatch.Stop();
            toolCallInfo.Status = ToolCallStatus.Failed;
            toolCallInfo.CompletedAt = DateTimeOffset.UtcNow;
            toolCallInfo.Error = ex.Message;

            _logger.LogWarning(ex, "Tool call failed: {FunctionName} ({ToolCallId})",
                context.Function.Name, toolCallId);

            // Broadcast failure to UI
            if (broadcasterScope != null)
            {
                await broadcasterScope.BroadcastCompletionAsync(toolCallInfo);
            }

            // Add failed tool call to collector for persistence
            if (logCollector != null)
            {
                logCollector.AddToolCall(toolCallInfo);
            }

            throw;
        }
    }

    /// <summary>
    /// Determines the tool call type based on function name and plugin patterns.
    /// </summary>
    internal static ToolCallType DetermineToolType(string functionName, string? pluginName)
    {
        var nameLower = functionName.ToLowerInvariant();

        // Check for document search tools
        if (nameLower.Contains("hybriddocumentsearch") ||
            nameLower.Contains("semanticdocumentsearch") ||
            nameLower.Contains("documentsearch"))
        {
            return ToolCallType.DocumentSearch;
        }

        // Check for web search tools
        if (nameLower.Contains("websearch") ||
            nameLower.Contains("web_search") ||
            nameLower.Contains("search_web"))
        {
            return ToolCallType.WebSearch;
        }

        // Check for image analysis tools
        if (nameLower.Contains("analyzeimage") ||
            nameLower.Contains("analyze_image") ||
            nameLower.Contains("imagecontent"))
        {
            return ToolCallType.ImageAnalysis;
        }

        // Check for summarization tools
        if (nameLower.Contains("summarize") ||
            nameLower.Contains("summary"))
        {
            return ToolCallType.Summarization;
        }

        // Check for MCP server tools (by plugin name pattern)
        if (!string.IsNullOrEmpty(pluginName) &&
            (pluginName.StartsWith("MCP", StringComparison.OrdinalIgnoreCase) ||
             pluginName.Contains("McpServer", StringComparison.OrdinalIgnoreCase)))
        {
            return ToolCallType.McpServer;
        }

        return ToolCallType.Other;
    }

    /// <summary>
    /// Extracts and serializes function arguments for display.
    /// </summary>
    private static Dictionary<string, string>? ExtractArguments(KernelArguments? arguments)
    {
        if (arguments == null || !arguments.Any())
            return null;

        var result = new Dictionary<string, string>();

        foreach (var name in arguments.Names)
        {
            var value = arguments[name];
            result[name] = SerializeArgument(value);
        }

        return result;
    }

    /// <summary>
    /// Serializes an argument value for display.
    /// </summary>
    internal static string SerializeArgument(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return s;

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            // Truncate long argument values
            return json.Length > 200 ? json[..200] + "..." : json;
        }
        catch
        {
            return value.ToString() ?? "null";
        }
    }

    /// <summary>
    /// Truncates the result for UI preview.
    /// </summary>
    internal static string? TruncateResult(object? result)
    {
        if (result == null) return null;

        string serialized;
        if (result is string s)
        {
            serialized = s;
        }
        else
        {
            try
            {
                serialized = JsonSerializer.Serialize(result, JsonOptions);
            }
            catch
            {
                serialized = result.ToString() ?? string.Empty;
            }
        }

        return serialized.Length > MaxResultLength
            ? serialized[..MaxResultLength] + "..."
            : serialized;
    }

    /// <summary>
    /// Injects a warning message into the function result when approaching the iteration limit.
    /// The warning is appended to the original result, preserving the tool's output while
    /// signaling to the LLM that it should synthesize a final answer.
    /// </summary>
    /// <param name="context">The auto function invocation context containing the result to modify.</param>
    private void InjectIterationWarning(AutoFunctionInvocationContext context)
    {
        int remaining = MaxAutoInvokeIterations - context.RequestSequenceIndex;
        string warningMessage = string.Format(
            IterationWarningMessage,
            context.RequestSequenceIndex,
            MaxAutoInvokeIterations,
            remaining);

        // Get current function result value and append warning
        var currentValue = context.Result?.GetValue<object?>();
        string modifiedResult = currentValue?.ToString() ?? string.Empty;
        modifiedResult += warningMessage;

        // Create a new FunctionResult wrapping the original but with the modified value
        // This preserves metadata while updating the string content
        context.Result = CreateModifiedFunctionResult(context.Result, modifiedResult);

        _logger.LogWarning(
            "Tool call iteration warning injected ({Current}/{Max}, {Remaining} remaining) for function: {FunctionName}",
            context.RequestSequenceIndex, MaxAutoInvokeIterations, remaining, context.Function.Name);
    }

    /// <summary>
    /// Creates a modified FunctionResult by wrapping the original with a new value.
    /// </summary>
    private static FunctionResult CreateModifiedFunctionResult(FunctionResult? original, string newValue)
    {
        // If original is null, create a new FunctionResult with just the value using the function from context
        // Otherwise, wrap the original to preserve metadata while updating the value
        // Note: original should never be null at this point since we're modifying an existing result
        return new FunctionResult(original!, newValue);
    }
}
