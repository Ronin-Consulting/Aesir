using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

public partial class AgentViewViewModel : ObservableRecipient, IDialogContext
{
    private AesirAgent _agent;
    
    private INotificationService _notificationService;
    
    [ObservableProperty] private AgentFormDataModel _formModel;
    
    public ObservableCollection<ModelSource> AvailableSources { get; } = new(Enum.GetValues<ModelSource>());
    
    public ObservableCollection<PromptContext> AvailablePrompts { get; } = new(Enum.GetValues<PromptContext>());
    
    public ObservableCollection<string> AvailableChatModels { get; set; }
    
    public ObservableCollection<string> AvailableEmbeddingModels { get; set; }
    
    public ObservableCollection<string> AvailableVisionModels { get; set; }
    
    public ObservableCollection<string> AvailableTools { get; set; }
    
    public bool IsDirty { get; set; }
    
    public ICommand SaveCommand { get; set; }
    
    public ICommand CancelCommand { get; set; }
    
    public ICommand DeleteCommand { get; set; }
    
    public event EventHandler<object?>? RequestClose;

    public AgentViewViewModel(AesirAgent agent, INotificationService notificationService)
    {
        _agent = agent;
        _notificationService = notificationService;
        
        FormModel = new()
        {
            Name = agent.Name,
            Source = agent.Source,
            Prompt = agent.Prompt,
            ChatModel = agent.ChatModel,
            EmbeddingModel = agent.EmbeddingModel,
            VisionModel = agent.VisionModel,
            Tools = new ObservableCollection<string>(agent.Tools)
        };
        IsDirty = false;
        SaveCommand = new RelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
        
        // TODO actually load - sources will determine the models
        AvailableChatModels = new ObservableCollection<string>()
        {
            "gpt-4.1-2025-04-14",
            "qwen3:32b-q4_K_M",
            "cogito:32b-v1-preview-qwen-q4_K_M"
        };
        AvailableEmbeddingModels = new ObservableCollection<string>()
        {
            "text-embedding-3-large",
            "mxbai-embed-large:latest"
        };
        AvailableVisionModels = new ObservableCollection<string>()
        {
            "gpt-4.1-2025-04-14",
            "gemma3:12b"
        };
        AvailableTools = new ObservableCollection<string>()
        {
            "RAG",
            "Web"
        };
    }

    private void ExecuteSaveCommand()
    {
        if (FormModel.Validate())
        {
            // TODO - apply FormModel to AesirAgent, store
            // TODO toast?
            _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' updated");
            Close();
        }
    }

    private void ExecuteCancelCommand()
    {
        Close();
    }

    private void ExecuteDeleteCommand()
    {
        // TODO - delete, toast?
        Close();
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }
}

public partial class AgentFormDataModel : ObservableValidator
{
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Name is required")] private string? _name;
    
    
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Source is required")] private ModelSource? _source;
    
    
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Prompt is required")] private PromptContext? _prompt;
    
    
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Chat Model is required")] private string? _chatModel;
    
    
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Embedding Model is required")] private string? _embeddingModel;
    
    
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Vision Model is required")] private string? _visionModel;

    [ObservableProperty] private ObservableCollection<string> _tools = new ObservableCollection<string>();

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }
}