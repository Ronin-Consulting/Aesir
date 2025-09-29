using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirKernelLogDetailsBase
{
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }
    
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }
    
    [JsonPropertyName("type")]
    public KernelLogType? Type { get; set; }
    
    [JsonPropertyName("arguments")]
    public List<KeyValuePair<string, string>>? Arguments { get; set; }
    
    [JsonPropertyName("function_name")]
    public string? FunctionName { get; set; }
    
    [JsonPropertyName("function_description")]
    public string? FunctionDescription { get; set; }
    
    [JsonPropertyName("plugin_name")]
    public string? PluginName { get; set; }
    
    [JsonPropertyName("underlying_method")]
    public string? UnderlyingMethod { get; set; }
}

public enum KernelLogType
{
    FunctionInvocation,
    AutoFunctionInvocation,
    PromptRender
}