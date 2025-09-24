using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;

namespace Aesir.Client.Services;

/// <summary>
/// Provides an interface for managing chat sessions and processing chat requests.
/// </summary>
public interface IChatSessionManager
{
    /// Asynchronously loads the current chat session into the application state. If no
    /// chat session is selected, a new instance of the chat session is created. Handles
    /// potential errors that may occur during the loading process by managing exceptions.
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LoadChatSessionAsync();

    /// Processes a chat request asynchronously by utilizing the specified agent, the ongoing conversation messages,
    /// and optional tool requests. Handles the orchestration of chat-related activities within the chat session.
    /// <param name="agentId">The identifier of the agent associated with the chat session.</param>
    /// <param name="conversationMessages">The collection of messages in the current conversation context.</param>
    /// <param name="tools">An optional array of tool requests to be incorporated during processing, if applicable.</param>
    /// <returns>A task that represents the asynchronous operation, returning the response as a string.</returns>
    Task<string> ProcessChatRequestAsync(Guid agentId, ObservableCollection<MessageViewModel?> conversationMessages,
        params ToolRequest[] tools);
}