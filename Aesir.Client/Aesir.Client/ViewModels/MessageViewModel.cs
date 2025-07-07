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
/// Represents the base view model for chat message handling in the application.
/// This is an abstract class that provides the common functionality and structure
/// required for managing messages in a conversation.
/// </summary>
public abstract partial class MessageViewModel : ObservableRecipient
{
    /// <summary>
    /// Logger instance used for logging messages and events within the class.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Provides functionality to render Markdown content as HTML.
    /// </summary>
    /// <remarks>
    /// This service is used to process Markdown content from chat messages
    /// and convert it into an HTML format for display.
    /// </remarks>
    /// <seealso cref="IMarkdownService"/>
    private readonly IMarkdownService _markdownService;

    /// <summary>
    /// Represents the internal storage for the message content associated with a MessageViewModel instance.
    /// </summary>
    /// <remarks>
    /// This field holds the actual message content and is used internally within the ViewModel for data binding and state management.
    /// It is decorated with the [ObservableProperty] attribute to automatically create observable properties for binding updates.
    /// </remarks>
    [ObservableProperty] 
    private string _message = string.Empty;

    /// <summary>
    /// Indicates whether the associated data or resource has been successfully loaded.
    /// </summary>
    /// <remarks>
    /// This field is used internally to track the state of loading operations
    /// within the <see cref="MessageViewModel"/> class or its derived types.
    /// It acts as a flag to determine if the associated content is ready for use.
    /// </remarks>
    [ObservableProperty] 
    private bool _isLoaded;

    /// Represents the role of the message entity. This property is used to categorize
    /// the type of message, such as "system", "user", or "assistant". The value of this
    /// property may vary depending on the specific type of the derived message view model.
    public virtual string Role => "Unknown";

    /// <summary>
    /// Represents the content of a message in the chat view model.
    /// Stores the textual content that can be updated, processed, and displayed in the application.
    /// </summary>
    /// <remarks>
    /// The <c>Content</c> property is utilized for managing the main message body.
    /// It is updated dynamically during operations such as message streaming or regeneration,
    /// and it may also undergo transformations, including markdown rendering or input normalization.
    /// </remarks>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the message.
    /// </summary>
    /// <remarks>
    /// This property is initialized with a new GUID by default when the instance is created.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Represents a command that can be executed to regenerate a message in the ViewModel.
    /// This property encapsulates the functionality to trigger a specific action or behavior
    /// associated with regenerating the message content within the respective ViewModel.
    /// Typically, used in conjunction with user interface elements to allow users to request
    /// regeneration of message content.
    /// </summary>
    /// <remarks>
    /// The specific implementation of the regeneration logic may vary depending on the
    /// derived class or context. This property is bound to UI components to enable interaction
    /// and is set during object construction through a method that defines the actual command behavior.
    /// </remarks>
    public ICommand RegenerateMessageCommand { get; }

    /// The MessageViewModel class represents a base implementation for handling messages in a conversation.
    /// It provides core properties and methods for managing message content, roles, and commands.
    /// Subclasses are expected to define specific behaviors by overriding provided methods.
    protected MessageViewModel(ILogger logger, IMarkdownService markdownService)
    {
        _logger = logger;
        _markdownService = markdownService;
        
        RegenerateMessageCommand = CreateRegenerateMessageCommand();
    }

    /// Creates the command responsible for triggering the regeneration of a message.
    /// This method is meant to be overridden in derived classes to provide custom behavior for the regenerate message command.
    /// <returns>
    /// An instance of ICommand that encapsulates the logic for regenerating a message.
    /// </returns>
    protected virtual ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(() => { }); // Default no-op implementation
    }

    /// <summary>
    /// Sets the message content and processes it into HTML format asynchronously.
    /// </summary>
    /// <param name="message">The message instance of type <c>AesirChatMessage</c> containing the content to set.</param>
    /// <returns>A <c>Task</c> representing the asynchronous operation of setting the message and rendering it.</returns>
    public virtual async Task SetMessage(AesirChatMessage message)
    {
        Content = message.Content;
        
        var htmlMessage = await _markdownService.RenderMarkdownAsHtmlAsync(NormalizeInput(message.Content));
        Message = htmlMessage;
        
        IsLoaded = true;
    }

    /// Asynchronously processes a streamed message and updates the content based on the received streamed results.
    /// This method collects and processes streamed data, managing the message content,
    /// formatting it, and providing a final title based on the processed data.
    /// <param name="message">
    /// An asynchronous enumerable containing streamed results of type <see cref="AesirChatStreamedResult"/>.
    /// </param>
    /// <returns>
    /// The title of the first valid received message, if available; otherwise, an empty string.
    /// </returns>
    public async Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message)
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

    /// <summary>
    /// Normalizes the provided input string.
    /// </summary>
    /// <param name="input">The input string to be normalized.</param>
    /// <returns>The normalized version of the input string.</returns>
    protected virtual string NormalizeInput(string input)
    {
        return input.Replace("\n", "<br>");
    }

    /// Returns an instance of AesirChatMessage constructed using the Role and Content properties of the current MessageViewModel instance.
    /// <returns>
    /// An AesirChatMessage object containing the Role and Content values of the current instance.
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