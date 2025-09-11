using System;
using System.Collections.Generic;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Models;

/// <summary>
/// Represents a chat session in the Aesir client. This class provides functionality for managing
/// messages within a chat conversation and includes default initialization for new chat sessions.
/// </summary>
public class AesirChatSession : AesirChatSessionBase
{
    /// <summary>
    /// Represents a chat session with client-specific defaults, containing metadata, messages, and conversation context.
    /// </summary>
    public AesirChatSession(PromptPersona? promptPersona, string? customContent)
    {
        Id = Guid.NewGuid();
        Title = "Chat Session (Client)";
        Conversation = new AesirConversation()
        {
            Id = Guid.NewGuid().ToString(),
            Messages = new List<AesirChatMessage>()
            {
                AesirChatMessage.NewSystemMessage(promptPersona, customContent)
            }
        };
    }

    /// <summary>
    /// Adds a new message to the current conversation if it is not already present.
    /// </summary>
    /// <param name="message">The chat message to be added to the conversation.</param>
    public void AddMessage(AesirChatMessage message)
    {
        if (Conversation.Messages.Contains(message)) return;
        
        Conversation.Messages.Add(message);
    }

    /// <summary>
    /// Removes a specified message from the conversation's message collection.
    /// </summary>
    /// <param name="message">The AesirChatMessage instance to be removed from the conversation.</param>
    public void RemoveMessage(AesirChatMessage message)
    {
        Conversation.Messages.Remove(message);
    }

    /// <summary>
    /// Retrieves the list of messages from the current conversation in the chat session.
    /// </summary>
    /// <returns>
    /// A list of messages representing the conversation history.
    /// </returns>
    public IList<AesirChatMessage> GetMessages()
    {
        return Conversation.Messages;
    }
}