using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the ViewModel specifically designed for handling messages created by users.
/// Inherits from <see cref="MessageViewModel"/> to provide additional user-specific message functionality.
/// </summary>
public partial class UserMessageViewModel(ILogger<UserMessageViewModel> logger, IMarkdownService markdownService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// Specifies whether the user message is currently being edited.
    /// </summary>
    [ObservableProperty] private bool _isEditing;

    /// <summary>
    /// Stores the raw, unprocessed text of the message. This field holds the original content
    /// before any formatting, parsing, or modifications are applied.
    /// </summary>
    [ObservableProperty] private string _rawMessage = string.Empty;

    /// <summary>
    /// Stores the name of a file associated with a user message within the view model.
    /// </summary>
    [ObservableProperty] private string? _fileName;

    [ObservableProperty] 
    private MaterialIconKind _fileIconKind = MaterialIconKind.FileDocument;
    
    /// <summary>
    /// An instance of the markdown service used to convert markdown text into rendered HTML content.
    /// </summary>
    private readonly IMarkdownService _markdownService1 = markdownService;

    /// <summary>
    /// Represents the role of the message sender.
    /// </summary>
    public override string Role => "user";

    /// <summary>
    /// Creates a command to execute the regenerate message functionality.
    /// </summary>
    /// <returns>An <see cref="ICommand"/> that initiates the regenerate message operation.</returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// <summary>
    /// Sets the message content by processing the input, including handling associated files,
    /// extracting content, and rendering it as HTML using a markdown service.
    /// </summary>
    /// <param name="message">The <see cref="Aesir.Common.Models.AesirChatMessage"/> instance containing the message data to be processed and displayed.</param>
    /// <returns>A task representing the asynchronous operation of setting and rendering the message content.</returns>
    public override async Task SetMessage(AesirChatMessage message)
    {
        if (message.HasFile())
        {
            FileName = message.GetFileName();
            
            if (Path.GetExtension(FileName ?? string.Empty).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                FileIconKind = MaterialIconKind.FileImage;
            }
        }

        Content = message.GetContentWithoutFileName() ?? throw new InvalidOperationException();

        var htmlMessage = await _markdownService1.RenderMarkdownAsHtmlAsync(Content);
        Message = htmlMessage;

        IsLoaded = true;
    }

    /// <summary>
    /// Updates the message content and its rendered HTML representation after being edited by the user.
    /// </summary>
    /// <param name="rawMessage">The raw user input for the message content.</param>
    /// <returns>A task that represents the asynchronous operation of updating the message content.</returns>
    public async Task SetMessageAfterEdit(string rawMessage)
    {
        Content = rawMessage;

        var htmlMessage = await _markdownService1.RenderMarkdownAsHtmlAsync(rawMessage);
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

    /// <summary>
    /// Converts the current message into a user-specific <see cref="AesirChatMessage"/> object.
    /// </summary>
    /// <returns>
    /// An <see cref="AesirChatMessage"/> instance with the role set to "user" and content consisting of the current message content.
    /// If a file name is associated with the message, it appends the file name within a file tag to the content.
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

    /// <summary>
    /// Converts HTML unordered list elements to plain text bullet point lists using a dash (-) as the bullet symbol.
    /// </summary>
    /// <param name="html">The input HTML string containing unordered list elements (<ul> and <li> tags).</param>
    /// <returns>A plain text string where the unordered list elements are represented as bullet point lists.</returns>
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
    /// Converts ordered list tags in an HTML string into plain text numbered lists with numerical prefixes.
    /// </summary>
    /// <param name="html">The HTML string containing ordered list tags to be processed.</param>
    /// <returns>A string in which ordered list tags are replaced by plain text numbered lists.</returns>
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

    /// <summary>
    /// Converts the provided HTML string into a plain text format, replacing specific HTML tags with textual equivalents.
    /// </summary>
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

    /// <summary>
    /// Sends a message to request the regeneration of the user's current message.
    /// Utilizes <see cref="WeakReferenceMessenger"/> to dispatch a <see cref="RegenerateMessageMessage"/>,
    /// which contains the current instance of the ViewModel as its content.
    /// External components subscribed to this message can handle the regeneration process
    /// for the user message as needed.
    /// </summary>
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
}