using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the ViewModel for managing user messages in the application.
/// Extends the <see cref="MessageViewModel"/> to provide specific functionality for user-generated messages.
/// </summary>
public partial class UserMessageViewModel(ILogger<UserMessageViewModel> logger, IMarkdownService markdownService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// Indicates whether the current message is in an editable state.
    /// </summary>
    [ObservableProperty] private bool _isEditing;

    /// <summary>
    /// Represents the raw message content in string format. This property is used to store and manipulate
    /// the unprocessed textual message data for a user.
    /// </summary>
    [ObservableProperty] private string _rawMessage = string.Empty;

    /// <summary>
    /// Represents the name of a file associated with the user message.
    /// </summary>
    [ObservableProperty] private string? _fileName;

    /// <summary>
    /// Represents an instance of the markdown service used to process and render markdown content as HTML.
    /// </summary>
    private readonly IMarkdownService _markdownService1 = markdownService;

    /// <summary>
    /// Gets the role associated with the message view model.
    /// </summary>
    /// <remarks>
    /// Overrides the base implementation to provide a specific role for the user message.
    /// This property returns "user" to indicate that the message originates from a user.
    /// </remarks>
    public override string Role => "user";

    /// <summary>
    /// Creates an <see cref="ICommand"/> that executes the regenerate message functionality.
    /// </summary>
    /// <returns>A command that triggers the regenerate message operation.</returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// <summary>
    /// Processes and sets the message content by handling associated files, normalizing input,
    /// and rendering the content to HTML using a markdown service.
    /// </summary>
    /// <param name="message">The <see cref="AesirChatMessage"/> instance containing the message data to process.</param>
    /// <returns>A task that represents the asynchronous operation. Upon completion, updates the view model's properties with the processed message content.</returns>
    public override async Task SetMessage(AesirChatMessage message)
    {
        if (message.HasFile())
        {
            FileName = message.GetFileName();
        }

        Content = message.GetContentWithoutFileName() ?? throw new InvalidOperationException();

        var htmlMessage = await _markdownService1.RenderMarkdownAsHtmlAsync(NormalizeInput(Content));
        Message = htmlMessage;

        IsLoaded = true;
    }

    /// Updates the message content and its rendered HTML representation after being edited by the user.
    /// <param name="rawMessage">The raw user input for the message content.</param>
    /// <returns>A task that represents the asynchronous operation of updating the message content.</returns>
    public async Task SetMessageAfterEdit(string rawMessage)
    {
        Content = rawMessage;

        var htmlMessage = await _markdownService1.RenderMarkdownAsHtmlAsync(NormalizeInput(rawMessage));
        Message = htmlMessage;

        IsLoaded = true;
    }

    /// <summary>
    /// Retrieves the current user message encapsulated as an instance of <see cref="AesirChatMessage"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="AesirChatMessage"/> instance representing the user's message with the appropriate role and content configured.
    /// </returns>
    public override AesirChatMessage GetAesirChatMessage()
    {
        return AsUserMessage();
    }

    /// Converts the current message's content into a user-specific AesirChatMessage object.
    /// If a file name is associated with the message, it includes the file name within a specific tag in the content.
    /// <returns>
    /// An AesirChatMessage object with the role set to "user" and the content set to the current message's content.
    /// If a file name is present, the content will include the file name in a specific format.
    /// </returns>
    public AesirChatMessage AsUserMessage()
    {
        var content = Content;

        if (FileName != null)
        {
            content = $"<file>{FileName}</file>{Content}";
        }

        return AesirChatMessage.NewUserMessage(content);
    }

    /// Converts HTML unordered list tags (<ul/> and <li/>) to plain text bullet point lists using a dash (-) to represent each list item.
    /// The method processes each <ul/> block separately, removing the <ul> and </ul> tags, and replacing <li/> tags with a dash (-) prefix.
    /// <param name="html">The input HTML string containing unordered lists.</param>
    /// <returns>A plain text representation of the input HTML with unordered lists converted to bullet point lists.</returns>
    public string ConvertUnorderedListTagsToBulletLists(string html)
    {
        var result = html;

        // Process each <ul> block individually
        while (result.Contains("<ul>"))
        {
            var ulStart = result.IndexOf("<ul>", StringComparison.InvariantCulture);
            var ulEnd = result.IndexOf("</ul>", ulStart, StringComparison.InvariantCulture);

            if (ulStart >= 0 && ulEnd >= 0)
            {
                var beforeUl = result[..ulStart];
                var ulContent = result[(ulStart + 4)..ulEnd];
                var afterUl = result[(ulEnd + 5)..];

                // Replace <li> tags with dash prefix within this ul block
                ulContent = ulContent.Replace("<li>", "- ").Replace("</li>", "");

                result = beforeUl + ulContent + afterUl;
            }
            else
            {
                break;
            }
        }

        result = result.Replace("<ul>", "").Replace("</ul>", "");

        return result;
    }

    /// <summary>
    /// Converts HTML ordered list tags (<ol/> and <li/>) into plain text numbered lists with numerical prefixes.
    /// </summary>
    /// <param name="html">The HTML string containing ordered list tags to be converted.</param>
    /// <returns>A string where ordered list tags are replaced with corresponding numbered lists in plain text format.</returns>
    public string ConvertOrderedListTagsToNumberedLists(string html)
    {
        var result = html;

        // Process each <ol> block individually
        while (result.Contains("<ol>"))
        {
            var olStart = result.IndexOf("<ol>", StringComparison.InvariantCulture);
            var olEnd = result.IndexOf("</ol>", olStart, StringComparison.InvariantCulture);

            if (olStart >= 0 && olEnd >= 0)
            {
                var beforeOl = result[..olStart];
                var olContent = result[(olStart + 4)..olEnd];
                var afterOl = result[(olEnd + 5)..];

                // Replace <li> tags with numbered prefix within this ol block
                var listNumber = 1;
                while (olContent.Contains("<li>"))
                {
                    var liIndex = olContent.IndexOf("<li>", StringComparison.InvariantCulture);
                    if (liIndex >= 0)
                    {
                        olContent = olContent[..liIndex] + $"{listNumber}. " + olContent[(liIndex + 4)..];
                    }

                    var closeLiIndex = olContent.IndexOf("</li>", StringComparison.InvariantCulture);
                    if (closeLiIndex >= 0)
                    {
                        olContent = olContent[..closeLiIndex] + olContent[(closeLiIndex + 5)..];
                    }

                    listNumber++;
                }

                result = beforeOl + olContent + afterOl;
            }
            else
            {
                break;
            }
        }

        result = result.Replace("<ol>", "").Replace("</ol>", "");

        return result;
    }

    /// Converts the provided HTML string into a plain text format, replacing specific HTML tags with textual equivalents.
    /// <param name="html">The HTML string to be converted into plain text.</param>
    /// <returns>A plain text representation of the HTML input, with lists converted and paragraph tags removed.</returns>
    public string ConvertFromHtml(string html)
    {
        // Can we go from html back to markdown instead...

        var result = html;

        // Convert HTML lists to plain text format
        result = ConvertUnorderedListTagsToBulletLists(result);
        result = ConvertOrderedListTagsToNumberedLists(result);

        // Remove paragraph tags
        result = result.Replace("<p>", "").Replace("</p>", "");

        return result.TrimEnd('\n');
    }

    /// Sends a message to regenerate the user's chat message. The method utilizes
    /// the WeakReferenceMessenger to dispatch a `RegenerateMessageMessage`, passing
    /// the current view model instance as the message content.
    /// This allows external components that are subscribed to the `RegenerateMessageMessage`
    /// to react and handle the regeneration of the user's message as needed.
    /// Usage of this method assumes that the receiving components properly handle the message
    /// and reprocess the user's input or regenerate the content accordingly.
    /// This method does not perform any UI update on its own nor does it modify the state of the
    /// view model directly. It only sends the regeneration request to be handled elsewhere.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
}