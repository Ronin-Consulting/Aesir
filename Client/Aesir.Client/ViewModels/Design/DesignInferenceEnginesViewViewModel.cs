using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Represents the design-time implementation of the <c>InferenceEnginesViewViewModel</c>.
/// Provides mock data and commands to aid in the design and development of the Inference Engine View interface.
/// </summary>
public class DesignInferenceEnginesViewViewModel : InferenceEnginesViewViewModel
{
    /// <summary>
    /// A design-time implementation of the <see cref="InferenceEnginesViewViewModel"/> class,
    /// primarily used for populating sample data and providing commands suitable for
    /// use during UI design in tools like visual editors.
    /// </summary>
    public DesignInferenceEnginesViewViewModel() : base(NullLogger<InferenceEnginesViewViewModel>.Instance, 
        new NoOpNavigationService(), new NoOpConfigurationService())
    {
        ShowChat = new RelayCommand(() => { });
        ShowAddInferenceEngine = new RelayCommand(() => { });
        
        var inferenceEngine = new List<AesirInferenceEngineBase> 
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Ollama 1",
                Description = "Default Ollama Engine",
                Type = InferenceEngineType.Ollama,
                Configuration = new Dictionary<string, string?>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Grok",
                Description = "Grok OpenAI Engine",
                Type = InferenceEngineType.OpenAICompatible,
                Configuration = new Dictionary<string, string?>()
            }
        };
        InferenceEngines = new ObservableCollection<AesirInferenceEngineBase>(inferenceEngine);
    }
}