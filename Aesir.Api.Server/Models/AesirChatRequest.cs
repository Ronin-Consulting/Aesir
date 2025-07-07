using Aesir.Common.Models;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents a chat completion request containing conversation data and model parameters.
/// </summary>
public class AesirChatRequest : AesirChatRequestBase
{
    /// <summary>
    /// Updates the system message to include the client's current date and time.
    /// </summary>
    public void SetClientDateTimeInSystemMessage()
    {
        var systemMessage = Conversation.Messages.FirstOrDefault(m => m.Role == "system");
        if (systemMessage != null)
        {
            systemMessage.Content = systemMessage.Content.Replace("{current_datetime}", ClientDateTime);
        }
    }
}