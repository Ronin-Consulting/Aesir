using System;
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
        ShowAddAgent = new RelayCommand(() => { });
        
        var agents = new List<AesirAgentBase> 
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Agent 1",
                Description = "Random agent 1",
                ChatModel = "gpt-4.1-2025-04-14",
                ChatInferenceEngineId = Guid.NewGuid(),
                VisionInferenceEngineId = Guid.NewGuid(),
                VisionModel = "gpt-4.1-2025-04-14",
                Prompt = PromptPersona.Military
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Agent 2",
                Description = "Random agent 2",
                ChatInferenceEngineId = Guid.NewGuid(),
                ChatModel = "qwen3:32b-q4_K_M",
                VisionInferenceEngineId = Guid.NewGuid(),
                VisionModel = "gemma3:12b",
                Prompt = PromptPersona.Military
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Computer Use",
                Description = "Agent that is allowed to take control of the computer",
                ChatInferenceEngineId = Guid.NewGuid(),
                ChatModel = "cogito:32b-v1-preview-qwen-q4_K_M",
                VisionInferenceEngineId = Guid.NewGuid(),
                VisionModel = "gemma3:12b",
                Prompt = PromptPersona.Business
            }
        };
        Agents = new ObservableCollection<AesirAgentBase>(agents);
    }
}