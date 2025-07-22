using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Client.ViewModels.Design;

public class DesignAgentsViewViewModel : AgentsViewViewModel
{   
    public DesignAgentsViewViewModel() : base(NullLogger<AgentsViewViewModel>.Instance, new NoOpNavigationService())
    {
        ShowChat = new RelayCommand(() => { });
        ShowTools = new RelayCommand(() => { });
        ShowAddAgent = new RelayCommand(() => { });
        
        var agents = new List<AesirAgent> 
        {
            new()
            {
                Name = "Agent 1",
                ChatModel = "gpt-4.1-2025-04-14",
                EmbeddingModel = "text-embedding-3-large",
                VisionModel = "gpt-4.1-2025-04-14",
                Source = "OpenAI",
                Tools = "ChatDocSearch_GetHybridKeywordSearchResults, ChatDocSearch_GetTextSearchResults",
                Prompt = "Military"
            },
            new()
            {
                Name = "Agent 2",
                ChatModel = "qwen3:32b-q4_K_M",
                EmbeddingModel = "mxbai-embed-large:latest",
                VisionModel = "gemma3:12b",
                Source = "Ollama",
                Tools = "ChatDocSearch_GetHybridKeywordSearchResults, ChatDocSearch_GetTextSearchResults",
                Prompt = "Military"
            },
            new()
            {
                Name = "Computer Use",
                ChatModel = "cogito:32b-v1-preview-qwen-q4_K_M",
                EmbeddingModel = "mxbai-embed-large:latest",
                VisionModel = "gemma3:12b",
                Source = "Ollama",
                Tools = "ChatDocSearch_GetHybridKeywordSearchResults, ChatDocSearch_GetTextSearchResults",
                Prompt = "Business"
            }
        };
        Agents = new ObservableCollection<AesirAgent>(agents);
    }
}