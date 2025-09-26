using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Controls;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ursa.Controls;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the base class for chat message view models, serving as a foundation for specialized message types.
/// Provides core properties and functionality to manage content, roles, behaviors, and integration within a chat application.
/// </summary>
public abstract partial class MessageViewModel : ObservableRecipient
{
    /// <summary>
    /// Private logger instance used to capture and record log messages and application events specific to the current view model.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Service instance used for converting and rendering Markdown content into an HTML format.
    /// </summary>
    /// <remarks>
    /// Provides functionality for transforming Markdown syntax, often used in user-generated
    /// chat messages, into HTML that can be displayed within the application interface.
    /// </remarks>
    private readonly IMarkdownService _markdownService;

    /// <summary>
    /// Represents the content of the message within the MessageViewModel.
    /// </summary>
    /// <remarks>
    /// This field supports property change notifications and is designed for use in data binding scenarios.
    /// It is initialized to an empty string and managed internally by the ViewModel.
    /// </remarks>
    [ObservableProperty] private string _message = string.Empty;

    /// <summary>
    /// Indicates whether the associated data or resource has been successfully loaded.
    /// </summary>
    /// <remarks>
    /// This field is used within the context of the <see cref="MessageViewModel"/> class to monitor and manage
    /// the completion status of loading operations. It reflects whether the required content or resource is fully ready for use.
    /// </remarks>
    [ObservableProperty] private bool _isLoaded;

    [ObservableProperty] private Guid? _ChatSessionId;
    
    /// <summary>
    /// Represents the role of a message in a conversation. Defines the specific function or identity
    /// a message assumes, such as "system", "user", or "assistant".
    /// </summary>
    public virtual string Role => "Unknown";

    /// <summary>
    /// Represents the textual content of a message within the chat system.
    /// Used to store and manage the core message data, such as user or assistant-generated text.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    public ICommand ShowLogDialogCommand { get; }
    
    private readonly IKernelLogService _kernelLogService;

    /// <summary>
    /// A unique identifier for the message instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Command responsible for triggering the regeneration of a chat message, allowing the content to be updated or reprocessed.
    /// </summary>
    public ICommand RegenerateMessageCommand { get; }

    /// <summary>
    /// Command that triggers the playback of the associated message's content.
    /// Typically used for playing audio versions of messages, if available.
    /// </summary>
    public ICommand PlayMessageCommand { get; }

    /// The MessageViewModel class is an abstract foundation for handling the representation and management of messages
    /// within a conversation. It provides essential properties and commands for functionalities like message content updates,
    /// playback, and regeneration. This class is intended to be extended by specific message types, enabling tailored behavior
    /// for different roles or scenarios within the application.
    protected MessageViewModel(ILogger logger, IMarkdownService markdownService,IKernelLogService kernelLogService)
    {
        _logger = logger;
        _markdownService = markdownService;
        _kernelLogService= kernelLogService;
        
        // ReSharper disable once VirtualMemberCallInConstructor
        RegenerateMessageCommand = CreateRegenerateMessageCommand();
        // ReSharper disable once VirtualMemberCallInConstructor
        PlayMessageCommand = CreatePlayMessageCommand();
        
        ShowLogDialogCommand = new AsyncRelayCommand(ExecuteShowLogDialog);
    }

    /// <summary>
    /// Shows a modal dialog to display logs on the conversation.
    /// </summary>
    private async Task ExecuteShowLogDialog()
    {
        var logs = await _kernelLogService.GetKernelLogsByChatSessionAsync(ChatSessionId);
        var model = new ConversationLogViewDialogViewModel();
        model.Logs = new ObservableCollection<AesirKernelLogBase>();
        foreach (var log in logs) model.Logs.Add(log);
        
        await OverlayDialog.ShowModal<LogViewDialog, ConversationLogViewDialogViewModel>(
            model,
            options: new OverlayDialogOptions()
            {
                Title = "Log View",
                Mode = DialogMode.Info,
                Buttons = DialogButton.OK,
                HorizontalAnchor = HorizontalPosition.Center,
                VerticalAnchor = VerticalPosition.Center,
                CanLightDismiss = true
            }
        );
    }

    /// Creates the command responsible for initiating the regeneration of a message in the view model context.
    /// This method can be overridden by derived classes to implement specific logic for regenerating a message.
    /// <returns>
    /// An instance of ICommand that encapsulates the logic for the regeneration process.
    /// </returns>
    protected virtual ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(() => { }); // Default no-op implementation
    }

    /// Creates a command responsible for triggering the playback of a message.
    /// This method provides a mechanism to initialize and retrieve an ICommand
    /// implementation that encapsulates the logic for playing message content.
    /// <returns>
    /// An ICommand instance designed to handle the execution of message playback functionality.
    /// </returns>
    protected virtual ICommand CreatePlayMessageCommand()
    {
        return new RelayCommand(() => { }); // Default no-op implementation
    }

    /// <summary>
    /// Asynchronously sets the message content and processes it into an appropriate format.
    /// </summary>
    /// <param name="message">An instance of <c>AesirChatMessage</c> containing the message data to be set and formatted.</param>
    /// <returns>A <c>Task</c> that represents the asynchronous operation of setting and processing the message content.</returns>
    public virtual async Task SetMessage(AesirChatMessage message)
    {
        Content = message.Content.TrimStart().NormalizeLineEndings();

        if (message.Role == "system")
        {
            Message = Content;
            IsLoaded = true;
            
            return;
        }   

        // Move markdown rendering to background thread to avoid UI blocking
        var htmlMessage = await Task.Run(() => _markdownService.RenderMarkdownAsHtmlAsync(Content, shouldRenderFencedCodeBlocks: true));
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Message = htmlMessage;
            IsLoaded = true;
        });
    }

    /// Asynchronously processes streamed messages to dynamically update content and extract relevant metadata.
    /// It collects and processes incoming data in real-time, modifying the message's content and attempting
    /// to determine a title if one is provided during the streaming process.
    /// <param name="message">
    /// An asynchronous enumerable of <see cref="AesirChatStreamedResult"/> objects, representing partial
    /// or complete results of the streamed chat message.
    /// </param>
    /// <returns>
    /// A task that resolves to a string containing the extracted title from the message stream,
    /// or an empty string if no title is identified.
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
                
                Content = Content.TrimStart().NormalizeLineEndings();
                
                // Render markdown on background thread to avoid UI blocking
                var htmlMessage = await Task.Run(() => _markdownService.RenderMarkdownAsHtmlAsync(Content));
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Message = htmlMessage;
                    IsLoaded = true;
                }, priority: DispatcherPriority.Input);
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