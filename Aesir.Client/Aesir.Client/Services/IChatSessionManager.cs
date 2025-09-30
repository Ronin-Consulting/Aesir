using System;
using System.Collections.Generic;
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

    /// Processes a chat request asynchronously using the specified agent, conversation messages, and optional parameters.
    /// This method coordinates interactions within a chat session, including invoking tools and managing optional thinking states.
    /// <param name="agentId">The unique identifier of the agent associated with the chat session.</param>
    /// <param name="conversationMessages">The collection of conversation messages defining the current session context.</param>
    /// <param name="tools">An optional collection of tool requests to include during processing.</param>
    /// <param name="enableThinking">An optional parameter to indicate whether the "thinking" feature should be enabled in the chat process.</param>
    /// <param name="thinkValue">An optional parameter representing a structured value to customize the thinking behavior.</param>
    /// <returns>A task that represents the asynchronous operation, returning the result of the processing as a string.</returns>
    Task<string> ProcessChatRequestAsync(Guid agentId, ObservableCollection<MessageViewModel?> conversationMessages,
        IEnumerable<ToolRequest>? tools = null, bool? enableThinking = null, ThinkValue? thinkValue = null);
}