using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.ViewModels;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpChatSessionManager : IChatSessionManager
{
    public async Task LoadChatSessionAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<string> ProcessChatRequestAsync(Guid agentId, ObservableCollection<MessageViewModel?> conversationMessages)
    {
        return await Task.FromResult("");
    }
}