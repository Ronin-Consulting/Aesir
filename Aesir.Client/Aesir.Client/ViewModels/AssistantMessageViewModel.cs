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

/// Represents the assistant's message in a conversation, inheriting functionality from the MessageViewModel class.
/// This class is specifically tailored for handling assistant-related operations such as regenerating responses,
/// processing streamed messages, and dealing with user interactions like link clicks. It incorporates logging,
/// markdown rendering, and content processing services to enhance its capabilities and ensure a dynamic user experience.
public partial class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// A readonly instance of the <see cref="IContentProcessingService"/> used to manage
    /// content handling operations within the assistant message view model.
    /// </summary>
    /// <remarks>
    /// This service facilitates functions such as processing content models and managing
    /// interactions, including actions triggered by user-initiated events like link clicks
    /// within the assistant messaging context.
    /// </remarks>
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ??
                                                                           throw new System.ArgumentNullException(
                                                                               nameof(contentProcessingService));

    /// <summary>
    /// A private field that stores the raw, unprocessed content of the assistant's internal thoughts
    /// extracted from an associated <see cref="AesirChatMessage"/>.
    /// </summary>
    /// <remarks>
    /// This field is dynamically updated as the assistant's streamed message evolves or when a new message
    /// is assigned. It represents the intermediate state of reasoning before being processed or rendered
    /// into a user-presentable format, such as HTML, for display in the user interface.
    /// </remarks>
    private string _thoughtsContent = string.Empty;

    /// <summary>
    /// A private, observable string property used to store the assistant's internal thoughts
    /// or reasoning related to the message being processed or displayed in the ViewModel.
    /// </summary>
    /// <remarks>
    /// This property provides additional context or insight regarding the assistant's
    /// response generation process. It serves as a means to capture and reflect
    /// reasoning or supplementary details intrinsic to the current interaction.
    /// </remarks>
    [ObservableProperty] private string _thoughtsMessage = string.Empty;

    /// <summary>
    /// A private field indicating whether the assistant is currently engaged in a thinking or processing state.
    /// </summary>
    /// <remarks>
    /// This field tracks the assistant's active computation status, allowing for appropriate updates to UI elements
    /// or behaviors that are contingent on the assistant's processing state.
    /// </remarks>
    [ObservableProperty] private bool _isThinking;

    /// <summary>
    /// A private, observable field indicating whether the assistant is actively engaged
    /// in generating or formulating its next response during a conversation or interaction.
    /// </summary>
    /// <remarks>
    /// This property is typically updated to reflect the assistant's ongoing thinking or
    /// message composition state, which may be relevant for UI updates or interaction workflow.
    /// </remarks>
    [ObservableProperty] private bool _isCollectingThoughts;

    /// <summary>
    /// Represents the role of the message sender within the conversation context.
    /// </summary>
    /// <remarks>
    /// The <see cref="Role"/> property specifies the identity or purpose of the sender
    /// (e.g., "assistant") to distinguish the assistant's messages from other types in the conversation.
    /// </remarks>
    public override string Role => "assistant";

    /// Generates the command responsible for initiating the regeneration of a message.
    /// This method ensures the appropriate command behavior is encapsulated to
    /// enable seamless handling of the regeneration process within the assistant's context.
    /// <returns>
    /// An instance of ICommand that contains the logic necessary to execute the regeneration
    /// of a message when invoked.
    /// </returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// Triggers the regeneration process for the assistant's message by sending a
    /// message of type RegenerateMessageMessage. This method facilitates the
    /// recalculation or updating of the assistant's response within the context
    /// of the current message instance.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }

    /// Handles the event where a link within the assistant message is clicked.
    /// This method delegates the processing of the clicked link to the content processing service,
    /// ensuring the link and its associated attributes are managed appropriately.
    /// <param name="link">The URL of the clicked link.</param>
    /// <param name="attributes">A dictionary containing additional attributes or metadata associated with the link.</param>
    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }

    /// Sets the provided message into the view model while ensuring the relevant processing,
    /// such as content conversion and state adjustments, is performed.
    /// This method updates the view model's data using the specified input message
    /// and invokes the base method for further processing.
    /// <param name="message">
    /// An instance of <see cref="AesirChatMessage"/> containing the data to be processed
    /// and displayed by the assistant.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation of setting the message.
    /// </returns>
    public override async Task SetMessage(AesirChatMessage message)
    {
        _thoughtsContent = message.ThoughtsContent;

        if (!string.IsNullOrWhiteSpace(_thoughtsContent))
        {
            var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(NormalizeInput(message.ThoughtsContent));
            ThoughtsMessage = htmlMessage;

            IsThinking = true;
        }

        await base.SetMessage(message);
    }

    /// Asynchronously processes a streamed message and updates the state of the view model accordingly.
    /// This method executes the logic necessary for handling chunks of streamed content,
    /// extracting relevant metadata such as the title and processing the message based on specified conditions.
    /// <param name="message">
    /// An asynchronous stream of <c>AesirChatStreamedResult?</c> objects representing parts of the streamed message
    /// along with their respective metadata and processing states.
    /// </param>
    /// <returns>
    /// A <c>Task</c> that represents the asynchronous operation, with the task result containing a string
    /// that represents the title extracted from the first applicable streamed message segment, if any.
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