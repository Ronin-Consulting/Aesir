using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.ViewModels;

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

    /// <summary>
    /// Processes a chat request asynchronously using the specified model and a collection of conversation messages.
    /// </summary>
    /// <param name="agentId">
    /// The id of the agent to be used for processing the chat request.
    /// </param>
    /// <param name="conversationMessages">
    /// A collection of <see cref="MessageViewModel"/> objects representing the conversation's messages.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the model's response as a string.
    /// </returns>
    Task<string> ProcessChatRequestAsync(Guid agentId, ObservableCollection<MessageViewModel?> conversationMessages);
}