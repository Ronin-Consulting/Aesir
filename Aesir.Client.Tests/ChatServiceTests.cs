using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Services.Implementations.Standard;
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

[TestClass]
public class ChatServiceTests
{
    private readonly IFlurlClientCache _flurlClientCache;
    private readonly IConfiguration _configuration;

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