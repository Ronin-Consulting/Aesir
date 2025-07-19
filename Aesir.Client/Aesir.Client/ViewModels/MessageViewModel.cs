using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Serves as the foundational abstract view model for all types of chat messages within the application.
/// Provides core properties and methods to handle message attributes, operations, and behaviors for different message roles in a conversation.
/// </summary>
public abstract partial class MessageViewModel : ObservableRecipient
{
    /// <summary>
    /// Logger instance for capturing and recording log messages and application events.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Service used to process and render Markdown content into HTML format.
    /// </summary>
    /// <remarks>
    /// Primarily utilized to handle the conversion of chat message content written in Markdown syntax
    /// into an HTML representation for display purposes.
    /// </remarks>
    private readonly IMarkdownService _markdownService;

    /// <summary>
    /// Stores the content of the message for the MessageViewModel instance.
    /// </summary>
    /// <remarks>
    /// This field is used internally by the ViewModel to manage the message content.
    /// Changes to this field trigger property change notifications for data binding purposes.
    /// It is initialized with an empty string and supports observation due to the [ObservableProperty] attribute.
    /// </remarks>
    [ObservableProperty] private string _message = string.Empty;

    /// <summary>
    /// Tracks the loading state of the associated data or resource.
    /// </summary>
    /// <remarks>
    /// This property determines whether the necessary content or resource has been fully loaded and is ready for use.
    /// It is primarily used within the <see cref="MessageViewModel"/> class to manage and monitor loading operations.
    /// </remarks>
    [ObservableProperty] private bool _isLoaded;

    /// <summary>
    /// Gets the role associated with the message, identifying its origin or type in a conversation context.
    /// </summary>
    public virtual string Role => "Unknown";

    /// <summary>
    /// Represents the message content associated with a chat message.
    /// This property holds the main textual content managed by the view model.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the message, represented as a GUID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Command used to regenerate or recompute a chat message. This command is typically
    /// invoked in scenarios where message content needs to be revised or refreshed.
    /// </summary>
    public ICommand RegenerateMessageCommand { get; }

    /// The MessageViewModel class serves as an abstract base for managing message-related functionality within conversations.
    /// It defines core properties such as content, role, and unique identifiers for messages, along with commands and methods
    /// for processing and updating message data. Derived classes can customize the behavior by overriding the virtual members.
    protected MessageViewModel(ILogger logger, IMarkdownService markdownService)
    {
        _logger = logger;
        _markdownService = markdownService;

        RegenerateMessageCommand = CreateRegenerateMessageCommand();
    }

    /// Creates the command responsible for triggering the regeneration of a message.
    /// This method is designed to be overridden in derived classes to define the specific logic
    /// for regenerating a message within the context of the respective view model.
    /// <returns>
    /// An object implementing the ICommand interface that handles the regeneration of a message.
    /// </returns>
    protected virtual ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(() => { }); // Default no-op implementation
    }

    /// <summary>
    /// Sets the message content and renders it into an appropriate format asynchronously.
    /// </summary>
    /// <param name="message">An instance of <c>AesirChatMessage</c> containing the content to be set and processed.</param>
    /// <returns>A <c>Task</c> representing the asynchronous operation of setting and processing the message content.</returns>
    public virtual async Task SetMessage(AesirChatMessage message)
    {
        Content = message.Content;

        var htmlMessage = await _markdownService.RenderMarkdownAsHtmlAsync(message.Content);
        Message = htmlMessage;

        IsLoaded = true;
    }

    /// Asynchronously processes a streamed message and dynamically updates the message content.
    /// This method collects the streamed results, builds the message content in real-time,
    /// and extracts a title if available during the streaming process.
    /// <param name="message">
    /// An asynchronous enumerable containing instances of <see cref="AesirChatStreamedResult"/> that represent
    /// partial or complete streamed results.
    /// </param>
    /// <returns>
    /// A string representing the extracted title of the first valid received message, or an empty string
    /// if no title could be determined.
    /// </returns>
    public virtual async Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message)
    {
        return await Task.Run(async () =>
        {
            var title = string.Empty;
            var hasReceivedTitle = false;
            Content = string.Empty;
            await foreach (var result in message)
            {
                if (result is null)
                {
                    continue;
                }

                //_logger.LogDebug("Received streamed message: {Result}", JsonSerializer.Serialize(result));

                // Only capture the first non-empty title we receive
                if (!hasReceivedTitle && !string.IsNullOrWhiteSpace(result.Title) && 
                    result.Title != "Chat Session (Server)" && result.Title != "Chat Session (Client)")
                {
                    title = result.Title;
                    hasReceivedTitle = true;
                }
                
                Content += result.Delta.Content;
                
                Content = Content.TrimStart();
                
                var htmlMessage = await _markdownService.RenderMarkdownAsHtmlAsync(Content);
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Message = htmlMessage;
                    IsLoaded = true;
                });
            }
            
            return title;
        });
    }

    /// Constructs and returns an instance of AesirChatMessage using the Role and Content properties of the current MessageViewModel.
    /// This method provides a standardized way to convert the MessageViewModel into a structured message format used by the Aesir chat system.
    /// <returns>
    /// An AesirChatMessage object initialized with the Role and Content values of the current MessageViewModel instance.
    /// </returns>
    public virtual AesirChatMessage GetAesirChatMessage()
    {
        return new AesirChatMessage()
        {
            Role = Role,
            Content = Content
        };
    }
}