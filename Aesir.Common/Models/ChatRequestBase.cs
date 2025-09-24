using System.Globalization;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class ChatRequestBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the chat session.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }
    
    /// <summary>
    /// Gets or sets the title of the chat session.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "title-not-set";
    
    /// <summary>
    /// Gets or sets the timestamp when the chat session was last updated.
    /// </summary>
    [JsonPropertyName("chat_session_updated_at")]
    public DateTimeOffset ChatSessionUpdatedAt { get; set; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Gets or sets the conversation containing the message history.
    /// </summary>
    [JsonPropertyName("conversation")]
    public AesirConversation Conversation { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the user identifier making the request.
    /// </summary>
    [JsonPropertyName("user")]
    public string User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the client's current date and time as a formatted string.
    /// </summary>
    [JsonPropertyName("client_datetime")]
    public string ClientDateTime { get; set; } = DateTime.Now.ToString("F", new CultureInfo("en-US"));
    
    /// <summary>
    /// Gets or sets a value indicating whether "thinking" behavior should be enabled for this chat request.
    /// </summary>
    [JsonPropertyName("enable_thinking")]
    public bool? EnableThinking { get; set; }

    /// <summary>
    /// Gets or sets the value that determines thinking behavior levels or states.
    /// It can represent either a boolean value or a string-based categorization (e.g., "high", "medium", "low").
    /// </summary>
    [JsonPropertyName("thinking_value")]
    public ThinkValue? ThinkValue { get; set; } = null!;

    public ICollection<ToolRequest> Tools { get; } = new HashSet<ToolRequest>();
    
        /// <summary>
    /// Adds a predefined web search tool to the collection of available tools for the chat request.
    /// </summary>
    /// <remarks>
    /// This method ensures the addition of a tool with the name specified by <c>AesirTools.WebSearchFunctionName</c>
    /// to the <c>Tools</c> collection. It allows extending the functionality of the chat request to include web search capabilities.
    /// </remarks>
    /// <returns>
    /// Returns the updated instance of the <c>AesirChatRequestBase</c>, enabling method chaining for further modifications.
    /// </returns>
    public ChatRequestBase AddWebTool()
    {
	    Tools.Add(ToolRequest.WebSearchToolRequest);

	    return this;
    }

    /// <summary>
    /// Removes the predefined web search tool from the collection of available tools for the chat request.
    /// </summary>
    /// <remarks>
    /// This method identifies the tool by the name <c>AesirTools.WebSearchFunctionName</c> and removes it from the <c>Tools</c> collection.
    /// It ensures that web search capabilities are no longer part of the chat request functionality.
    /// </remarks>
    /// <returns>
    /// Returns the updated instance of the <c>AesirChatRequestBase</c>, enabling method chaining for further modifications.
    /// </returns>
    public ChatRequestBase RemoveWebTool()
    {
	    Tools.Remove(Tools.First(t => t.ToolName == AesirTools.WebToolName));
	    
	    return this;
    }

    /// <summary>
    /// Adds a tool to the collection of available tools for the chat request.
    /// </summary>
    /// <param name="toolRequest">
    /// The <c>AesirTool</c> instance to be added to the <c>Tools</c> collection.
    /// </param>
    /// <returns>
    /// Returns the updated instance of the <c>AesirChatRequestBase</c>, enabling method chaining for further modifications.
    /// </returns>
    public ChatRequestBase WithTool(ToolRequest toolRequest)
    {
	    Tools.Add(toolRequest);

	    return this;
    }

    /// <summary>
    /// Removes a specified tool from the collection of available tools for the chat request.
    /// </summary>
    /// <param name="name">
    /// The name of the tool to be removed from the <c>Tools</c> collection.
    /// </param>
    /// <returns>
    /// Returns the updated instance of the <c>AesirChatRequestBase</c>, enabling method chaining for further modifications.
    /// </returns>
    public ChatRequestBase RemoveTool(string name)
    {
	    Tools.Remove(Tools.First(t => t.ToolName == name));
	    
	    return this;
    }
}