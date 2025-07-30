using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

public class DesignAgentViewViewModel : AgentViewViewModel
{   
    public DesignAgentViewViewModel() : base(new AesirAgentBase()
    {
        Name = "My Test Agent",
        Source = ModelSource.Ollama,
        Prompt = PromptContext.Military,
        ChatModel = "qwen3:32b-q4_K_M",
        EmbeddingModel = "mxbai-embed-large:latest",
        VisionModel = "gemma3:12b"
    }, new NoOpNotificationService(), new NoOpConfigurationService())
    {
        AvailableTools = new ObservableCollection<string>()
        {
            "RAG",
            "Web"
        };
        IsDirty = false;
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
    }
}