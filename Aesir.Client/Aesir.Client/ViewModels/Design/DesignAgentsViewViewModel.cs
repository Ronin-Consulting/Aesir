using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Represents the design-time implementation of the <c>AgentsViewViewModel</c>.
/// Provides mock data and commands to aid in the design and development of the Agents View interface.
/// </summary>
public class DesignAgentsViewViewModel : AgentsViewViewModel
{
    /// <summary>
    /// A design-time implementation of the <see cref="AgentsViewViewModel"/> class,
    /// primarily used for populating sample data and providing commands suitable for
    /// use during UI design in tools like visual editors.
    /// </summary>
    public DesignAgentsViewViewModel() : base(NullLogger<AgentsViewViewModel>.Instance, new NoOpNavigationService(), new NoOpConfigurationService())
    {
        ShowChat = new RelayCommand(() => { });
        ShowTools = new RelayCommand(() => { });
        ShowAddAgent = new RelayCommand(() => { });
        
        var agents = new List<AesirAgentBase> 
        {
            new()
            {
                Name = "Agent 1",
                ChatModel = "gpt-4.1-2025-04-14",
                EmbeddingModel = "text-embedding-3-large",
                VisionModel = "gpt-4.1-2025-04-14",
                Source = ModelSource.OpenAI,
                Prompt = PromptContext.Military
            },
            new()
            {
                Name = "Agent 2",
                ChatModel = "qwen3:32b-q4_K_M",
                EmbeddingModel = "mxbai-embed-large:latest",
                VisionModel = "gemma3:12b",
                Source = ModelSource.Ollama,
                Prompt = PromptContext.Military
            },
            new()
            {
                Name = "Computer Use",
                ChatModel = "cogito:32b-v1-preview-qwen-q4_K_M",
                EmbeddingModel = "mxbai-embed-large:latest",
                VisionModel = "gemma3:12b",
                Source = ModelSource.Ollama,
                Prompt = PromptContext.Business
            }
        };
        Agents = new ObservableCollection<AesirAgentBase>(agents);
    }
}