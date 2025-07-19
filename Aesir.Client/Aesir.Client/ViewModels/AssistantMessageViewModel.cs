using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// Represents the assistant's message in a conversation, deriving from the MessageViewModel class.
/// This class handles assistant-specific functionalities such as managing regenerated responses, processing streamed content,
/// and supporting interactions like handling link clicks. It leverages logging, markdown formatting, and advanced content
/// processing services to ensure a responsive and engaging user interaction experience.
public partial class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// A readonly instance of the <see cref="IContentProcessingService"/> utilized for managing
    /// content processing tasks related to the assistant message view model functionality.
    /// </summary>
    /// <remarks>
    /// This instance supports operations such as processing and managing content behaviors
    /// and handles interactions resulting from user-triggered events, such as link clicks.
    /// </remarks>
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ??
                                                                           throw new System.ArgumentNullException(
                                                                               nameof(contentProcessingService));

    /// <summary>
    /// A private field that stores the raw content representing the assistant's internal thoughts
    /// extracted from incoming messages such as <see cref="AesirChatMessage"/>.
    /// </summary>
    /// <remarks>
    /// This field is updated dynamically during the lifecycle of a message, either when assigning
    /// new content or streaming partial updates. It serves as a pre-rendered state of the assistant's
    /// reasoning process before being processed into a user-facing representation.
    /// </remarks>
    private string _thoughtsContent = string.Empty;

    /// <summary>
    /// A private observable string property that holds the assistant's internal thoughts
    /// or reasoning associated with the current message being processed or displayed.
    /// </summary>
    /// <remarks>
    /// This property is intended to provide additional insights or supplementary information
    /// regarding the assistant's thought process during response generation. It captures
    /// contextual reasoning that may enhance the understanding of the assistant's behavior
    /// within the ongoing interaction.
    /// </remarks>
    [ObservableProperty] private string _thoughtsMessage = string.Empty;

    /// <summary>
    /// A private field indicating whether the assistant is currently engaged in a thinking or processing state.
    /// </summary>
    /// <remarks>
    /// This field tracks the assistant's active computation status, enabling responsive updates to the user interface
    /// or related application behaviors that depend on the assistant's processing activity.
    /// </remarks>
    [ObservableProperty] private bool _isThinking;

    /// <summary>
    /// A private, observable field indicating whether the assistant is in the process of
    /// formulating or generating its response during an interaction.
    /// </summary>
    /// <remarks>
    /// Changes to this field signify the assistant's active state of thought collection,
    /// typically used to modify UI elements or manage interaction workflows.
    /// </remarks>
    [ObservableProperty] private bool _isCollectingThoughts;

    /// <summary>
    /// Represents the role of the message sender within the conversation context.
    /// </summary>
    /// <remarks>
    /// Specifies the identity or purpose of the sender, such as "assistant," to differentiate
    /// this sender's messages from others in the messaging flow.
    /// </remarks>
    public override string Role => "assistant";

    /// Generates a command responsible for triggering a message regeneration process.
    /// This method encapsulates the behavior required to facilitate the regeneration
    /// workflow within the assistant's operational context.
    /// <returns>
    /// An ICommand instance containing the logic to execute the regeneration of a
    /// message when invoked.
    /// </returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// Initiates the regeneration of the assistant's message by triggering the necessary
    /// messaging framework to send a `RegenerateMessageMessage` that corresponds to the
    /// current message instance. This method is responsible for updating or recalculating
    /// the assistant's response dynamically within the existing conversation context.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }

    /// Handles the event triggered when a hyperlink within the assistant's message is clicked.
    /// This method processes the link and its associated metadata using the content processing service.
    /// <param name="link">The URL of the clicked hyperlink.</param>
    /// <param name="attributes">A dictionary containing key-value pairs representing additional metadata for the link.</param>
    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }

    /// Sets the specified message into the view model, performing any required
    /// adjustments such as content formatting, state updates, and processing logic,
    /// while ensuring base functionality is invoked as part of the operation.
    /// <param name="message">
    /// An instance of <see cref="AesirChatMessage"/> containing the data to be integrated
    /// and displayed within the assistant view model.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the message-setting logic.
    /// </returns>
    public override async Task SetMessage(AesirChatMessage message)
    {
        _thoughtsContent = message.ThoughtsContent ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(_thoughtsContent))
        {
            var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(_thoughtsContent);
            ThoughtsMessage = htmlMessage;

            IsThinking = true;
        }

        await base.SetMessage(message);
    }

    /// Asynchronously processes a streamed message and updates the corresponding view model state.
    /// This method handles incoming message chunks, performs necessary transformations, and extracts key information,
    /// such as a title, from the data stream to update the view model accordingly.
    /// <param name="message">
    /// An asynchronous stream of <c>AesirChatStreamedResult?</c> objects that provides message segments
    /// along with relevant metadata for processing.
    /// </param>
    /// <returns>
    /// A <c>Task</c> that represents the asynchronous operation. The result is a string containing
    /// the title derived from the first relevant chunk of the streamed message, if applicable.
    /// </returns>
    public override async Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message)
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

                var isThinking = result.IsThinking;

                if (isThinking)
                {
                    _thoughtsContent += result.Delta.ThoughtsContent;

                    _thoughtsContent = _thoughtsContent.TrimStart();

                    var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(_thoughtsContent);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsThinking = isThinking;
                        IsCollectingThoughts = true;

                        ThoughtsMessage = htmlMessage;
                        IsLoaded = true;
                    });
                }
                else
                {
                    Content += result.Delta.Content;

                    Content = Content.TrimStart();

                    var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(Content);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsCollectingThoughts = false;

                        Message = htmlMessage;
                        IsLoaded = true;
                    });
                }

                // let ui catch up drawing
                await Task.Delay(TimeSpan.FromMilliseconds(30));
            }

            return title;
        });
    }
}