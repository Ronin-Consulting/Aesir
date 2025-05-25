using System.Globalization;
using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

public class AesirChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "model-not-set";

    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "title-not-set";

    [JsonPropertyName("chat_session_updated_at")]
    public DateTimeOffset ChatSessionUpdatedAt { get; set; } = DateTimeOffset.Now;

    [JsonPropertyName("conversation")]
    public AesirConversation Conversation { get; set; } = null!;

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("user")]
    public string User { get; set; } = null!;

    [JsonPropertyName("client_datetime")]
    public string ClientDateTime { get; set; } = DateTime.Now.ToString("F", new CultureInfo("en-US"));

    public void SetClientDateTimeInSystemMessage()
    {
        var systemMessage = Conversation.Messages.FirstOrDefault(m => m.Role == "system");
        if (systemMessage != null)
        {
            systemMessage.Content = systemMessage.Content.Replace("{current_datetime}", ClientDateTime);
        }
    }
}