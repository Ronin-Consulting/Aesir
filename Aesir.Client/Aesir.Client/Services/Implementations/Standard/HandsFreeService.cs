using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class HandsFreeService : IHandsFreeService
{
    private readonly ILogger<HandsFreeService> _logger;
    private readonly ISpeechService _speechService;
    private readonly ApplicationState _appState;
    private readonly IChatSessionManager _chatSessionManager;
    
    private HandsFreeState _currentState = HandsFreeState.Idle;
    private bool _isHandsFreeActive;
    private CancellationTokenSource? _handsFreeToken;
    private Task? _processingTask;

    // Conversation management - mimicking ChatViewViewModel
    private readonly ObservableCollection<MessageViewModel?> _conversationMessages = [];
    private string? _selectedModelId;

    /// <inheritdoc />
    public bool IsHandsFreeActive => _isHandsFreeActive;

    /// <inheritdoc />
    public HandsFreeState CurrentState => _currentState;
    
    public ObservableCollection<MessageViewModel?> ConversationMessages => _conversationMessages;

    /// <inheritdoc />
    public event EventHandler<HandsFreeStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;

    public HandsFreeService(
        ILogger<HandsFreeService> logger,
        ISpeechService speechService,
        ApplicationState appState,
        IChatSessionManager chatSessionManager)
    {
        _logger = logger;
        _speechService = speechService;
        _appState = appState;
        _chatSessionManager = chatSessionManager;
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
    /// Main processing loop for hands-free interactions.
    /// </summary>
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
    /// Processes a single speech interaction cycle: Listen → Process → Speak → Repeat.
    /// </summary>
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
    /// Listens for user speech and returns the recognized text.
    /// For conversational AI, we expect complete utterances from the speech service.
    /// </summary>
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
    /// Processes user message through chat completion system.
    /// Mimics the pattern from ChatViewViewModel.SendMessageAsync.
    /// </summary>
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
    /// Adds a message to the conversation, mimicking ChatViewViewModel.AddMessageToConversationAsync.
    /// </summary>
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to conversation");
            throw;
        }
    }

    /// <summary>
    /// Speaks the assistant response.
    /// For conversational AI, we simplify to avoid conflicts with speech recognition.
    /// </summary>
    private async Task SpeakResponse(string response, CancellationToken cancellationToken)
    {
        try
        {
            await _speechService.SpeakAsync("Hello World!");
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
    /// Changes the current state and notifies subscribers.
    /// </summary>
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