using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;

namespace Aesir.Client.ViewModels.Interfaces;

public interface IMessageViewModel
{
    string Role { get; }
    string Content { get; set; }
    string Message { get; set; }
    bool IsLoaded { get; set; }
    
    Task SetMessage(AesirChatMessage message);
    Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message);
    AesirChatMessage GetAesirChatMessage();
}