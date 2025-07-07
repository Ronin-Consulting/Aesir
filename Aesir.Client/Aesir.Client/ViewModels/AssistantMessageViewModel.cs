using System.Collections.Generic;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// The AssistantMessageViewModel class represents the assistant's message in a conversation.
/// It is responsible for handling the logic specific to assistant messages, including commands,
/// content normalization, and link interactions.
public class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// A readonly instance of the <see cref="IContentProcessingService"/> used for processing and handling
    /// content-related logic within the ViewModel.
    /// </summary>
    /// <remarks>
    /// This service performs operations such as processing content for a thinking model and handling
    /// user interactions, such as link clicks, within the assistant's functionalities.
    /// </remarks>
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ??
                                                                           throw new System.ArgumentNullException(
                                                                               nameof(contentProcessingService));

    /// <summary>
    /// Gets the role associated with the message represented by the view model.
    /// This property defines the type of the message, distinguishing it as belonging
    /// to a specific role in the system, such as "assistant" or "system". The default
    /// role is defined in the base class as "Unknown".
    /// </summary>
    public override string Role => "assistant";

    /// Creates the command responsible for triggering the regeneration of a message.
    /// This method overrides the base implementation to provide a specific command
    /// for handling the regenerate message operation, typically tied to an assistant's behavior.
    /// <returns>
    /// An instance of ICommand that encapsulates the logic for regenerating a message.
    /// The returned command will invoke the RegenerateMessage method when executed.
    /// </returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// Regenerates the assistant message by sending a `RegenerateMessageMessage`
    /// containing the current instance of the `AssistantMessageViewModel`.
    /// This method is invoked internally by the `RegenerateMessageCommand`
    /// to handle requests for re-generating or refreshing the assistant's response.
    /// Typically used in cases where the previous message needs to be refreshed
    /// or recalculated.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }

    /// <summary>
    /// Normalizes the input string by processing it through the content processing service.
    /// </summary>
    /// <param name="input">The input string to be normalized.</param>
    /// <returns>A normalized version of the input string.</returns>
    protected override string NormalizeInput(string input)
    {
        return _contentProcessingService.ProcessThinkingModelContent(input);
    }

    /// Handles the event of a link being clicked within an assistant message.
    /// <param name="link">The URL of the link that was clicked.</param>
    /// <param name="attributes">A dictionary containing additional attributes associated with the link.</param>
    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }
}