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
/// the hands-free mode, enabling voice-based interaction and providing accessibility
/// features for users within the application.
/// </summary>
/// <remarks>
/// This service is responsible for activating and deactivating hands-free mode,
/// monitoring its current state, and handling voice input processing. It leverages
/// dependencies like <see cref="ISpeechService"/> for managing speech recognition and
/// integrates with <see cref="ApplicationState"/> for obtaining the application's context.
/// Additionally, it provides events to notify state changes, audio level updates,
/// and recognized utterances, allowing seamless integration with other components.
/// </remarks>
/// <example>
/// Typical use cases for this class include enabling voice commands, conversational
/// interfaces, or hands-free accessibility features in applications.
/// </example>
public class HandsFreeService : IHandsFreeService
{
    /// <summary>
    /// Logger instance used for recording diagnostic information, warnings, errors, and state changes
    /// within the <see cref="HandsFreeService"/> class.
    /// </summary>
    private readonly ILogger<HandsFreeService> _logger;

    /// <summary>
    /// Instance of <see cref="ISpeechService"/> used to handle speech recognition and synthesis
    /// processes within the hands-free functionality of the application.
    /// </summary>
    private readonly ISpeechService _speechService;

    /// <summary>
    /// Represents the shared application state utilized by the <see cref="HandsFreeService"/>
    /// to manage and retrieve application-wide data and configurations.
    /// </summary>
    private readonly ApplicationState _appState;

    /// <summary>
    /// Responsible for managing chat sessions and facilitating the processing of chat requests
    /// within the hands-free service operations.
    /// </summary>
    private readonly IChatSessionManager _chatSessionManager;

    /// <summary>
    /// Service instance for processing and rendering markdown content.
    /// Utilized within the <see cref="HandsFreeService"/> class for converting markdown into
    /// plain text for scenarios such as text-to-speech processing.
    /// </summary>
    private readonly IMarkdownService _markdownService;

    /// <summary>
    /// Represents the current operational state of the hands-free service, indicating
    /// whether the service is idle, listening, processing, speaking, or in an error state.
    /// </summary>
    private HandsFreeState _currentState = HandsFreeState.Idle;

    /// <summary>
    /// Indicates whether the hands-free mode is currently active within the <see cref="HandsFreeService"/>.
    /// This variable helps manage hands-free mode state and ensures that concurrent activation or deactivation
    /// attempts are handled appropriately.
    /// </summary>
    private bool _isHandsFreeActive;

    /// <summary>
    /// Cancellation token source used to control and cancel ongoing operations
    /// associated with the hands-free mode within the <see cref="HandsFreeService"/> class.
    /// </summary>
    private CancellationTokenSource? _handsFreeToken;

    /// <summary>
    /// Represents a task that manages the asynchronous processing logic for hands-free mode
    /// within the <see cref="HandsFreeService"/> class. This task is responsible for executing
    /// the core flow of the hands-free operation and runs until the mode is stopped.
    /// It is initialized when hands-free mode starts and set to null when the mode is inactive.
    /// </summary>
    private Task? _processingTask;

    // Conversation management - mimicking ChatViewViewModel
    /// <summary>
    /// Represents a collection of conversation messages utilized in the hands-free interaction feature
    /// for tracking and handling exchanges between the user and the assistant.
    /// This collection includes messages from various roles such as user, assistant, and system.
    /// </summary>
    private readonly ObservableCollection<MessageViewModel?> _conversationMessages = [];

    /// <summary>
    /// Holds the unique identifier of the currently selected agent used during hands-free operations
    /// and chat processing within the <see cref="HandsFreeService"/> class.
    /// This variable is updated based on the application state and is required to be
    /// valid (non-null and non-whitespace) for operations dependent on the selected agent.
    /// </summary>
    private Guid? _selectedAgentId;

    /// <summary>
    /// Indicates whether the hands-free mode is currently active in the <see cref="HandsFreeService"/> class.
    /// </summary>
    public bool IsHandsFreeActive => _isHandsFreeActive;

    /// <summary>
    /// Represents the current operational state of the hands-free service,
    /// defined as a value of the <see cref="HandsFreeState"/> enumeration.
    /// </summary>
    public HandsFreeState CurrentState => _currentState;

    /// <summary>
    /// Collection of conversation messages represented as <see cref="MessageViewModel"/> objects.
    /// This property provides access to the messages being exchanged in the hands-free mode
    /// within the <see cref="HandsFreeService"/>.
    /// </summary>
    public ObservableCollection<MessageViewModel?> ConversationMessages => _conversationMessages;

    /// <summary>
    /// Event that is triggered whenever the hands-free state changes within the
    /// <see cref="HandsFreeService"/> class. Provides information regarding the
    /// previous state, current state, and any associated error messages.
    /// </summary>
    public event EventHandler<HandsFreeStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event triggered when the audio level changes, providing details
    /// about the new audio level within the <see cref="HandsFreeService"/> class.
    /// </summary>
    public event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;

    /// <summary>
    /// Event triggered when an utterance text is recognized during the speech processing cycle
    /// within the <see cref="HandsFreeService"/> class.
    /// </summary>
    public event EventHandler<UtteranceTextEventArgs>? UtteranceTextRecognized;

    /// <summary>
    /// Implements hands-free operation functionalities using speech synthesis, recognition, and state management.
    /// </summary>
    /// <remarks>
    /// This class collaborates with various services, such as <see cref="ISpeechService"/> for voice operations,
    /// <see cref="ApplicationState"/> for managing the application's state, and <see cref="IChatSessionManager"/> for handling chat session coordination.
    /// It is designed to facilitate seamless hands-free interactions, including monitoring audio levels and tracking application state transitions.
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

    /// <summary>
    /// Initiates the hands-free mode by activating necessary components, setting the selected agent,
    /// and starting the hands-free processing loop.
    /// </summary>
    /// <remarks>
    /// This method first verifies the current state to prevent duplicate activation of hands-free mode.
    /// It initializes the required cancellation token, retrieves the selected agent from the application's state,
    /// and transitions to the initial idle state. Upon successful setup, the processing loop for hands-free mode is started.
    /// If an error occurs during initialization, the state is updated to reflect the error and the exception is logged or propagated.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
            
            // Get the selected agent from current app state
            _selectedAgentId = _appState.SelectedAgent?.Id;
            if (_selectedAgentId == null)
            {
                throw new InvalidOperationException("No agent selected. Please select an agent before starting hands-free mode.");
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
    /// Stops the hands-free mode if it is currently active, performing cleanup operations and resetting
    /// the state to idle. Ensures proper state management and logs any relevant information or encountered errors.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of stopping hands-free mode.</returns>
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
    /// Executes the main processing loop for managing hands-free operations, handling user interactions and system state transitions.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that signals the request to cancel the processing loop, enabling graceful termination.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous execution of the hands-free processing loop.
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
    /// Processes a single speech interaction cycle, including listening for user speech, processing the input, generating a response, and speaking the response.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for task cancellation, allowing the operation to be terminated prematurely.</param>
    /// <returns>A task that represents the asynchronous operation of the speech interaction cycle.</returns>
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

                var utteranceTextEventArgs = new UtteranceTextEventArgs()
                {
                    Text = utterance
                };
            
                UtteranceTextRecognized?.Invoke(this, utteranceTextEventArgs);
                
                userMessage.Append(utterance);
            }
            
            if (string.IsNullOrWhiteSpace(userMessage.ToString()))
            {
                // No speech detected, continue listening
                await ChangeStateAsync(HandsFreeState.Idle);
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

            // Step 3: Speak the response
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
    /// Listens for user speech asynchronously and retrieves a collection of recognized speech fragments.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor and handle task cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of recognized speech fragments as strings.</returns>
    private async Task<IList<string>> ListenForUserSpeechAsync(CancellationToken cancellationToken)
    {
        // stop on silence
        // ReSharper disable once ConvertToLocalFunction
        Func<int, bool> shouldStopIfSilence = millisecondsOfSilence =>
        {
            if (TimeSpan.FromMilliseconds(millisecondsOfSilence) > TimeSpan.FromMilliseconds(750))
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
    /// Processes a user message through the chat completion system, enabling the generation of a conversational response
    /// based on the provided input and the current state of the chat session.
    /// </summary>
    /// <param name="userMessage">The input message from the user to be processed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the assistant's response as a string, or an error message if processing fails.</returns>
    private async Task<string> ProcessChatCompletion(string userMessage, CancellationToken cancellationToken)
    {
        try
        {
            if (_selectedAgentId == null)
            {
                throw new InvalidOperationException("No agent selected");
            }

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
            await _chatSessionManager.ProcessChatRequestAsync(
                _selectedAgentId.Value, _conversationMessages);

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
    /// Asynchronously adds a message to the conversation by creating and configuring the corresponding view model,
    /// and updating the conversation collection.
    /// </summary>
    /// <param name="message">The <see cref="AesirChatMessage"/> representing the message to be added to the conversation.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of the asynchronous operation.</returns>
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
    /// Speaks the assistant's response using text-to-speech functionality.
    /// Handles potential interruptions, converts markdown to plain text, and manages
    /// the text-to-speech process lifecycle throughout the operation.
    /// </summary>
    /// <param name="response">The response text to be converted to speech and spoken by the assistant.</param>
    /// <param name="cancellationToken">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation of speaking the given response.</returns>
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
    /// <param name="errorMessage">
    /// An optional error message, relevant when transitioning to the <see cref="HandsFreeState.Error"/> state.
    /// </param>
    /// <returns>A task that represents the asynchronous operation of changing the state.</returns>
    private async Task ChangeStateAsync(HandsFreeState newState, string? errorMessage = null)
    {
        if (newState == HandsFreeState.Error)
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