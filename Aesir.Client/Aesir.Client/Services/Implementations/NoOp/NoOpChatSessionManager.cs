using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpChatSessionManager : IChatSessionManager
{
    public async Task LoadChatSessionAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<string> ProcessChatRequestAsync(Guid agentId, ObservableCollection<MessageViewModel?> conversationMessages, IEnumerable<ToolRequest>? tools = null,
        bool? enableThinking = null, ThinkValue? thinkValue = null)
    {
        return await Task.FromResult("");
    }
}