using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// The <see cref="HandsFreeService"/> class provides functionality to manage and control
/// hands-free mode interactions, which includes voice-based input and interaction handling
/// within the application.
/// </summary>
/// <remarks>
/// This service is used to start, stop, and monitor the state of hands-free mode.
/// It uses the <see cref="ISpeechService"/> for speech-related tasks and integrates
/// with the <see cref="ApplicationState"/> to get application context.
/// Events for state and audio level changes are also provided to enable event-driven mechanisms.
/// </remarks>
/// <example>
/// This class is generally used in scenarios where hands-free interaction is required,
/// such as voice commands or conversational interfaces.
/// </example>
public class HandsFreeService : IHandsFreeService
{
    /// <summary>
    /// Logger instance used for logging messages, warnings, errors, and informational events
    /// within the <see cref="HandsFreeService"/> class.
    /// </summary>
    private readonly ILogger<HandsFreeService> _logger;

    /// <summary>
    /// Provides access to the speech service for speech recognition and synthesis functionalities
    /// within the hands-free mode of the application.
    /// </summary>
    private readonly ISpeechService _speechService;

    /// <summary>
    /// Represents the shared application state utilized by the hands-free service
    /// to access and manage application-wide configurations or states.
    /// </summary>
    private readonly ApplicationState _appState;

    /// <summary>
    /// Manages chat sessions and processes chat requests, facilitating interactions
    /// within the hands-free service workflow.
    /// </summary>
    private readonly IChatSessionManager _chatSessionManager;

    private readonly IMarkdownService _markdownService;

    /// <summary>
    /// Represents the current operational state of the hands-free service.
    /// </summary>
    private HandsFreeState _currentState = HandsFreeState.Idle;

    /// Indicates whether hands-free mode is currently active within the HandsFreeService.
    /// This field is used internally to track the status of the hands-free mode and prevent
    /// multiple activation or deactivation attempts concurrently.
    private bool _isHandsFreeActive;

    /// <summary>
    /// Represents a cancellation token source used to manage the lifecycle of the hands-free mode operation.
    /// </summary>
    private CancellationTokenSource? _handsFreeToken;

    /// Represents a task that handles the hands-free processing loop asynchronously.
    /// It is initiated when the hands-free mode starts and manages the core processing logic
    /// until the mode is stopped, at which point the task completes.
    /// This is set to null when no hands-free processing is active.
    private Task? _processingTask;

    // Conversation management - mimicking ChatViewViewModel
    /// <summary>
    /// Stores the collection of conversation messages used in the hands-free interaction flow.
    /// Manages the messages exchanged between the user and the assistant, including user inputs and assistant responses.
    /// </summary>
    private readonly ObservableCollection<MessageViewModel?> _conversationMessages = [];

    /// Represents the identifier of the currently selected model used during hands-free operations.
    /// This variable is assigned from the current application state to ensure that a valid model
    /// is selected before starting hands-free mode or processing chat requests.
    /// It is expected to be non-null and non-whitespace when functionality dependent on it is executed.
    private string? _selectedModelId;

    /// Indicates whether the hands-free mode is currently active.
    public bool IsHandsFreeActive => _isHandsFreeActive;

    /// <summary>
    /// Gets the current state of the hands-free service.
    /// </summary>
    public HandsFreeState CurrentState => _currentState;

    /// <summary>
    /// Provides access to the collection of conversation messages in the hands-free service.
    /// This collection is an observable sequence of <see cref="MessageViewModel" /> instances
    /// that represent individual messages within a conversation.
    /// </summary>
    public ObservableCollection<MessageViewModel?> ConversationMessages => _conversationMessages;

    /// <summary>
    /// Event triggered when the hands-free state changes.
    /// </summary>
    public event EventHandler<HandsFreeStateChangedEventArgs>? StateChanged;

    /// Event triggered when the audio level changes.
    public event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;

    /// <summary>
    /// Provides an implementation of the <see cref="IHandsFreeService"/> interface, enabling hands-free operation functionality.
    /// </summary>
    /// <remarks>
    /// The <see cref="HandsFreeService"/> class integrates with <see cref="ISpeechService"/> for speech synthesis and recognition,
    /// the application's current state via <see cref="ApplicationState"/>, and manages chat sessions with <see cref="IChatSessionManager"/>.
    /// It also provides events to track state changes and monitor audio levels during hands-free operations.
    /// </remarks>
    public HandsFreeService(
        ILogger<HandsFreeService> logger,
        ISpeechService speechService,
        ApplicationState appState,
        IChatSessionManager chatSessionManager,
        IMarkdownService markdownService)
    {
        _logger = logger;
        _speechService = speechService;
        _appState = appState;
        _chatSessionManager = chatSessionManager;
        _markdownService = markdownService;

        if (appState.ChatSession == null) return;
        
        foreach (var message in appState.ChatSession.GetMessages())
        {
            switch (message.Role)
            {
                case "user":
                    var userMessage = Ioc.Default.GetService<UserMessageViewModel>();
                    _ = userMessage!.SetMessage(message);
                    
                    _conversationMessages.Add(userMessage);
                    break;
                case "assistant":
                    var assistantMessage = Ioc.Default.GetService<AssistantMessageViewModel>();
                    _ = assistantMessage!.SetMessage(message);
                    
                    _conversationMessages.Add(assistantMessage);
                    break;
                case "system":
                    var systemMessage = Ioc.Default.GetService<SystemMessageViewModel>();
                    _ = systemMessage!.SetMessage(message);
                    
                    _conversationMessages.Add(systemMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(message.Role);
            }
        }
    }

    /// <inheritdoc />
    public async Task StartHandsFreeMode()
    {
        if (_isHandsFreeActive)
        {
            _logger.LogWarning("Hands-free mode is already active");
            return;
        }

        try
        {
            _handsFreeToken = new CancellationTokenSource();
            
            // Get the selected model from current app state
            _selectedModelId = _appState.SelectedModel!.Id;
            if (string.IsNullOrWhiteSpace(_selectedModelId))
            {
                throw new InvalidOperationException("No model selected. Please select a model before starting hands-free mode.");
            }

            _isHandsFreeActive = true;
            await ChangeStateAsync(HandsFreeState.Idle);

            // Start processing task
            _processingTask = ProcessHandsFreeLoop(_handsFreeToken.Token);

            _logger.LogInformation("Hands-free mode started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start hands-free mode");
            await ChangeStateAsync(HandsFreeState.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Stops the hands-free mode if it is currently active. Performs necessary cleanup and resets
    /// the state to idle. Logs relevant information or errors during the operation.
    /// </summary>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopHandsFreeMode()
    {
        if (!_isHandsFreeActive)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping hands-free mode");
            
            _isHandsFreeActive = false;
            await _handsFreeToken!.CancelAsync();

            if (_processingTask != null)
            {
                await _processingTask;
            }

            await ChangeStateAsync(HandsFreeState.Idle);
            _logger.LogInformation("Hands-free mode stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping hands-free mode");
        }
        finally
        {
            _handsFreeToken?.Dispose();
            _handsFreeToken = null;
            _processingTask = null;
        }
    }

    /// <summary>
    /// Executes the main processing loop for managing hands-free interactions.
    /// </summary>
    /// <param name="cancellationToken">
    /// A CancellationToken to observe while waiting for the loop to process.
    /// This allows the operation to be cancelled gracefully.
    /// </param>
    /// <returns>
    /// A Task representing the asynchronous operation of the hands-free processing loop.
    /// </returns>
    private async Task ProcessHandsFreeLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _isHandsFreeActive)
            {
                await ProcessSpeechCycle(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Hands-free processing loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hands-free processing loop");
            await ChangeStateAsync(HandsFreeState.Error, ex.Message);
        }
    }

    /// <summary>
    /// Processes a single speech interaction cycle, consisting of the following stages:
    /// Listening for user speech, processing the input, generating a response, and speaking the response.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete, used to cancel the operation if required.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    private async Task ProcessSpeechCycle(CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Listen for user speech
            await ChangeStateAsync(HandsFreeState.Listening);

            var userMessage = new StringBuilder();
            foreach (var utterance in await ListenForUserSpeechAsync(cancellationToken))
            {
                _logger.LogDebug("User speech detected: {Speech}", utterance);
                userMessage.Append(utterance);
            }
            
            if (string.IsNullOrWhiteSpace(userMessage.ToString()))
            {
                // No speech detected, continue listening
                return;
            }

            // Step 2: Process with chat completion (mimicking ChatViewViewModel.SendMessageAsync)
            await ChangeStateAsync(HandsFreeState.Processing);
            var assistantResponse = await ProcessChatCompletion(userMessage.ToString(), cancellationToken);
            
            if (string.IsNullOrWhiteSpace(assistantResponse))
            {
                _logger.LogWarning("Empty response from chat completion");
                return;
            }

            // Step 3: Speak the response (with interruption detection)
            await ChangeStateAsync(HandsFreeState.Speaking);
            await SpeakResponse(assistantResponse, cancellationToken);

        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in speech cycle");
            await ChangeStateAsync(HandsFreeState.Error, ex.Message);
            
            // Brief pause before returning to listening
            await Task.Delay(2000, cancellationToken);
        }
    }

    /// <summary>
    /// Listens for user speech asynchronously and returns a list of recognized text fragments.
    /// This method utilizes the speech service to process user input, stopping on silence or cancellation.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings representing recognized speech fragments.</returns>
    private async Task<IList<string>> ListenForUserSpeechAsync(CancellationToken cancellationToken)
    {
        // stop on silence
        // ReSharper disable once ConvertToLocalFunction
        Func<int, bool> shouldStopIfSilence = millisecondsOfSilence =>
        {
            if (TimeSpan.FromMilliseconds(millisecondsOfSilence) > TimeSpan.FromSeconds(1))
            {
                _logger.LogDebug("Stopping speech recognition due to silence");
                return true;
            }

            return false;
        };

        // stop if canceled
        cancellationToken.Register(async () =>
        {
            _logger.LogDebug("Stopping speech recognition due to cancellation");
            await _speechService.StopListeningAsync();
        });

        return await _speechService.ListenAsync(shouldStopIfSilence);
    }

    /// <summary>
    /// Processes the user message through the chat completion system and generates an assistant response.
    /// </summary>
    /// <param name="userMessage">The message provided by the user that will be processed.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the assistant's response as a string.</returns>
    private async Task<string> ProcessChatCompletion(string userMessage, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedModelId))
            {
                throw new InvalidOperationException("No model selected");
            }

            // Mimic ChatViewViewModel.SendMessageAsync pattern:
            
            // 1. Add user message to conversation
            var userChatMessage = AesirChatMessage.NewUserMessage(userMessage);
            await AddMessageToConversationAsync(userChatMessage);

            // 2. Add placeholder for assistant response
            var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
            if (assistantMessageViewModel == null)
            {
                throw new InvalidOperationException("Could not resolve AssistantMessageViewModel");
            }

            _conversationMessages.Add(assistantMessageViewModel);

            // 3. Process the chat request
            await _chatSessionManager.ProcessChatRequestAsync(_selectedModelId, _conversationMessages);

            // 4. Extract the response text from the assistant message
            var responseText = assistantMessageViewModel.Content ?? "I apologize, but I couldn't generate a response.";
            
            _logger.LogInformation("Chat completion processed successfully");
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat completion");
            return "I apologize, but there was an error processing your request.";
        }
    }

    /// <summary>
    /// Asynchronously adds a message to the conversation by creating and adding the appropriate view model and updating the conversation list.
    /// </summary>
    /// <param name="message">The <see cref="AesirChatMessage"/> instance representing the message to be added to the conversation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task AddMessageToConversationAsync(AesirChatMessage message)
    {
        try
        {
            var userMessageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
            if (userMessageViewModel == null)
            {
                throw new InvalidOperationException("Could not resolve UserMessageViewModel");
            }

            userMessageViewModel.SetMessage(message);
            _conversationMessages.Add(userMessageViewModel);

            _logger.LogDebug("Added user message to conversation");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to conversation");
            throw;
        }
    }

    /// <summary>
    /// Speaks the assistant response using text-to-speech capabilities.
    /// This method ensures the response speech is processed for conversational AI,
    /// handling interruptions and managing the text-to-speech service lifecycle.
    /// </summary>
    /// <param name="response">The response text to be spoken by the assistant.</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous speak operation.</returns>
    private async Task SpeakResponse(string response, CancellationToken cancellationToken)
    {
        try
        {
            var plainTextResponse = await _markdownService.RenderMarkdownAsPlainTextAsync(response);
            await _speechService.SpeakAsync(plainTextResponse);
            _logger.LogDebug("AI response speech completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TTS playback");
        }
        finally
        {
            await _speechService.StopSpeakingAsync();
        }
    }

    /// <summary>
    /// Changes the current hands-free state and notifies subscribers about the state transition.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    /// <param name="errorMessage">An optional error message, relevant when transitioning to the <see cref="HandsFreeState.Error"/> state.</param>
    /// <returns>A task that represents the asynchronous operation of changing the state.</returns>
    private async Task ChangeStateAsync(HandsFreeState newState, string? errorMessage = null)
    {
        if(newState == HandsFreeState.Error)
            await _handsFreeToken!.CancelAsync();
        
        var previousState = _currentState;
        _currentState = newState;

        _logger.LogDebug("State changed from {PreviousState} to {NewState}", previousState, newState);

        var args = new HandsFreeStateChangedEventArgs
        {
            PreviousState = previousState,
            CurrentState = newState,
            ErrorMessage = errorMessage
        };

        StateChanged?.Invoke(this, args);
    }
}