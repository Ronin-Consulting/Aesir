using Aesir.Client.Models;
using Aesir.Client.Services.Implementations.Standard;
using Aesir.Common.Models;
using FluentAssertions;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Aesir.Client.Tests;

/// <summary>
/// Unit test class for testing functionalities of the ChatService implementation.
/// </summary>
/// <remarks>
/// This class covers integration tests for ChatService with a focus on various request types
/// such as standard chat completions and streaming chat completions. The tests validate correct
/// behavior through arranged scenarios and expected outcomes by making use of mocks and
/// dependency injections.
/// </remarks>
/// <example>
/// - Ensuring the `ChatCompletionsAsync` method returns valid responses when processing
/// non-streamed chat completion requests.
/// - Validating the `ChatCompletionsStreamedAsync` method for correct handling
/// of streamed chat completion requests.
/// </example>
/// <seealso cref="ChatService"/>
/// <seealso cref="AesirChatRequest"/>
[TestClass]
public class ChatServiceTests
{
    /// <summary>
    /// Represents a cache of Flurl HTTP clients used to manage and reuse instances of IFlurlClient.
    /// This aids in improving performance by reducing the overhead of creating and disposing
    /// of HTTP client instances for repeated HTTP requests within the test suite.
    /// </summary>
    private readonly IFlurlClientCache _flurlClientCache;

    /// <summary>
    /// Represents the configuration settings used in the test class for initializing and configuring various components,
    /// such as service dependencies and environment-specific parameters.
    /// </summary>
    /// <remarks>
    /// This variable is used to provide values for application settings, specifically for dependency injection
    /// purposes during the initialization of services (e.g., URLs or other configurations for ChatService).
    /// It is built using a ConfigurationBuilder with an in-memory collection of key-value pairs.
    /// </remarks>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Provides unit tests for the ChatService implementation. The tests validate the behavior
    /// of ChatService methods to ensure correct functionality when interacting with chat-related features.
    /// </summary>
    public ChatServiceTests()
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5, fastFirst: true);

        var policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(delay);
        
        _flurlClientCache = new FlurlClientCache().WithDefaults(builder =>
            builder
                .WithTimeout(60)
                .AddMiddleware(() => new PolicyHttpMessageHandler(policy))
        );

        _configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    {"Inference:Chat", "http://localhost:5131/chat/completions"}
                }).Build();
    }

    /// <summary>
    /// Tests the ChatCompletionsAsync method of the ChatService class by sending a sample chat request
    /// and verifying that a valid response is returned.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation of the test, ensuring the result is not null
    /// and the ChatCompletionsAsync method works as expected.
    /// </returns>
    [TestMethod]
    public async Task Test_ChatCompletionsAsync()
    {
        // Arrange
        var logger = new Mock<ILogger<ChatService>>();
        
        var chatService = new ChatService(logger.Object, _configuration, _flurlClientCache);
        var request = new AesirChatRequest()
        {
            Model = "llama-3-8b",
            Conversation = new AesirConversation()
            {
                Id = "TestingConversation",
                Messages = new List<AesirChatMessage>()
                {
                    AesirChatMessage.NewSystemMessage("You are a helpful assistant."),
                    AesirChatMessage.NewUserMessage("How far from the Sun is the earth?")
                }
            },
            Temperature = 0.2,
            TopP = 1.0,
            MaxTokens = 4096,
            User = "TestingUser"
        };

        Console.WriteLine(JsonConvert.SerializeObject(request));
        
        // Act
        var result = await chatService.ChatCompletionsAsync(request);
        
        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests the <see cref="ChatService.ChatCompletionsStreamedAsync"/> method to ensure it properly streams
    /// chat completion results based on a provided <see cref="AesirChatRequest"/> object.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous test operation. The task contains assertions verifying that:
    /// the result is not null, and each streamed completion is non-null as well.
    /// </returns>
    [TestMethod]
    public async Task Test_ChatCompletionsStreamedAsync()
    {
        // Arrange
        var logger = new Mock<ILogger<ChatService>>();
        
        var chatService = new ChatService(logger.Object, _configuration, _flurlClientCache);
        var request = new AesirChatRequest()
        {
            Model = "llama-3-8b",
            Conversation = new AesirConversation()
            {
                Id = "TestingConversation",
                Messages = new List<AesirChatMessage>()
                {
                    AesirChatMessage.NewSystemMessage("You are a helpful assistant."),
                    AesirChatMessage.NewUserMessage("How far from the Sun is the earth?")
                }
            },
            Temperature = 0.2,
            TopP = 1.0,
            MaxTokens = 4096,
            User = "TestingUser"
        };

        var json = JsonConvert.SerializeObject(request);
        Console.WriteLine(json);
        
        // Act
        var result = chatService.ChatCompletionsStreamedAsync(request);
        
        // Assert
        result.Should().NotBeNull();
        
        await foreach (var item in result)
        {
            item.Should().NotBeNull();
            
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] {JsonConvert.SerializeObject(item)}");
        }
    }
}