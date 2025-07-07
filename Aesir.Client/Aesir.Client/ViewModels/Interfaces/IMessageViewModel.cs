using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;

namespace Aesir.Client.ViewModels.Interfaces;

/// <summary>
/// Represents the interface for a message view model, defining the properties and methods for managing messages in the client view model.
/// </summary>
public interface IMessageViewModel
{
    /// <summary>
    /// Represents the role associated with a message or entity.
    /// This property typically defines the type or classification of the sender or owner
    /// of the message, such as "user", "assistant", or "system".
    /// </summary>
    string Role { get; }

    /// <summary>
    /// Gets or sets the textual content associated with the implementing object.
    /// This property is used to hold the main conversational or informational text,
    /// typically in the context of a chat system where the content could represent
    /// a user's input, assistant's response, or system-specific messages.
    /// </summary>
    string Content { get; set; }

    /// <summary>
    /// Gets or sets the message content associated with the view model.
    /// This property represents the primary text content or information
    /// handled by the implemented <see cref="IMessageViewModel"/> interface.
    /// </summary>
    string Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the necessary data has been successfully loaded
    /// into the current view model instance.
    /// </summary>
    bool IsLoaded { get; set; }

    /// <summary>
    /// Sets the message for the view model using the provided <see cref="AesirChatMessage"/>.
    /// </summary>
    /// <param name="message">
    /// The message of type <see cref="AesirChatMessage"/> to be set in the view model.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task SetMessage(AesirChatMessage message);

    /// <summary>
    /// Asynchronously processes a streamed chat message and returns the accumulated content as a string.
    /// </summary>
    /// <param name="message">An asynchronous enumerable of <see cref="AesirChatStreamedResult"/> instances to be processed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the accumulated message content as a string.</returns>
    Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message);

    /// Retrieves the current AesirChatMessage instance, typically representing
    /// a message with properties such as role and content.
    /// <returns>
    /// An instance of the AesirChatMessage class representing the current message.
    /// </returns>
    AesirChatMessage GetAesirChatMessage();
}