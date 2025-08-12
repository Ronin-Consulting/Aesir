using System;
using Aesir.Common.Models;

namespace Aesir.Client.Models;

/// <summary>
/// Represents a client-specific request for initiating a chat in the Aesir system.
/// </summary>
/// <remarks>
/// The AesirChatRequest class serves as the primary data model for chat requests,
/// inheriting shared core properties and behaviors from AesirChatRequestBase.
/// It provides functionality to initialize with client-specific default values and
/// includes a factory method to create a request preconfigured with sensible defaults.
/// </remarks>
/// <seealso cref="AesirChatRequestBase"/>
public class AesirChatRequest : AesirChatRequestBase
{
    /// <summary>
    /// Represents a chat request for the Aesir client, containing essential parameters
    /// such as model selection, conversation details, and customization options
    /// for generating chat completions.
    /// </summary>
    public AesirChatRequest()
    {
        Temperature = 0.1;
        MaxTokens = 8192;
    }

    /// <summary>
    /// Creates a new instance of the AesirChatRequest class with predefined default values.
    /// </summary>
    /// <returns>A new AesirChatRequest object configured with default parameters, such as model, temperature, maximum tokens, user, and a unique conversation ID.</returns>
    public static AesirChatRequest NewWithDefaults()
    {
        return new AesirChatRequest()
        {
            Model = "not-set",
            Conversation = new AesirConversation()
            {
                Id = Guid.NewGuid().ToString()
            },
            Temperature = 0.1,
            MaxTokens = 8192,
            User = "Unknown"
        };
    }
}