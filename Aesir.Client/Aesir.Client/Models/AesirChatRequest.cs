using System;
using Aesir.Common.Models;

namespace Aesir.Client.Models;

public class AesirChatRequest : AesirChatRequestBase
{
    /// <summary>
    /// Initializes a new instance of the AesirChatRequest class with client-specific defaults.
    /// </summary>
    public AesirChatRequest()
    {
        Temperature = 0.1;
        MaxTokens = 8192;
    }
    
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