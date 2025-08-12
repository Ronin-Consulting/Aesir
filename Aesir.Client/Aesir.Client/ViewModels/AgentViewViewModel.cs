using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for agent-related views, providing properties, commands,
/// and events to manage agent configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> and implements <see cref="IDialogContext"/>,
/// enabling it to work with MVVM patterns and dialog-based user interactions. It manages
/// collections of available models, tools, and prompts, as well as command bindings for
/// managing agent configuration lifecycle actions such as save, cancel, and delete.
/// </remarks>
/// <example>
/// Typically used in the context of agent detail views within the application. The view model
/// is activated and bound to the associated user control for displaying and managing agent
/// data.
/// </example>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IDialogContext" />
public partial class AgentViewViewModel : ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Represents the underlying agent configuration and details used by the view model, including properties such as ID, name, models, and associated tools.
    /// </summary>
    private AesirAgentBase _agent;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including tools
    /// and agents, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Represents the form data model for the agent view, used to handle data
    /// binding and validation within the AgentViewViewModel.
    /// </summary>
    [ObservableProperty] private AgentFormDataModel _formModel;

    /// <summary>
    /// Collection of available model sources that can be selected or used within the application.
    /// </summary>
    public ObservableCollection<ModelSource> AvailableSources { get; } = new(Enum.GetValues<ModelSource>());

    /// <summary>
    /// Collection of available prompts that defines the context in which the agent operates.
    /// </summary>
    public ObservableCollection<PromptPersona> AvailablePrompts { get; } = new(Enum.GetValues<PromptPersona>());

    /// <summary>
    /// Collection of available chat models that can be used.
    /// </summary>
    public ObservableCollection<string> AvailableChatModels { get; set; }

    /// <summary>
    /// Collection of available embedding models that can be used or selected in the application.
    /// </summary>
    public ObservableCollection<string> AvailableEmbeddingModels { get; set; }

    /// <summary>
    /// Collection of available vision models that can be selected or used within the application.
    /// </summary>
    public ObservableCollection<string> AvailableVisionModels { get; set; }

    /// <summary>
    /// Collection of tools available for selection in the agent configuration.
    /// </summary>
    public ObservableCollection<string> AvailableTools { get; set; }

    /// <summary>
    /// Indicates whether the view model has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Command executed to save the current state or data of the agent view.
    /// </summary>
    public ICommand SaveCommand { get; set; }

    /// <summary>
    /// Command used to cancel the current operation or revert changes in the Agent View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Command used to delete the associated agent or entity.
    /// </summary>
    public ICommand DeleteCommand { get; set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;

    /// Represents the view model for the agent view. Handles the binding of agent data
    /// and communication between the user interface and underlying services.
    public AgentViewViewModel(AesirAgentBase agent, 
            INotificationService notificationService,
            IConfigurationService configurationService)
    {
        _agent = agent;
        _notificationService = notificationService;
        _configurationService = configurationService;
        
        FormModel = new()
        {
            Name = agent.Name,
            Source = agent.Source,
            Prompt = agent.Prompt,
            ChatModel = agent.ChatModel,
            EmbeddingModel = agent.EmbeddingModel,
            VisionModel = agent.VisionModel,
            Tools = new ObservableCollection<string>()
        };
        IsDirty = false;
        SaveCommand = new RelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new RelayCommand(ExecuteDeleteCommand);

        AvailableChatModels = new ObservableCollection<string>();
        AvailableEmbeddingModels = new ObservableCollection<string>();
        AvailableVisionModels = new ObservableCollection<string>();
        AvailableTools = new ObservableCollection<string>();
    }

    /// <summary>
    /// Invoked when the view model is activated. This method executes initialization logic such as
    /// invoking UI-thread-specific tasks and loading the necessary resources or state for operation.
    /// </summary>
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadAvailableAsync);
    }

    /// <summary>
    /// Asynchronously loads and initializes available tools, model sources, and models for the agent.
    /// This method retrieves available tools through the configuration service, updates relevant observable collections
    /// with the data, and configures the tools associated with the current agent.
    /// Clears and repopulates available collections for chat models, embedding models, and vision models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of loading data.</returns>
    private async Task LoadAvailableAsync()
    {
        try
        {
            // get available tools
            var availableTools = await _configurationService.GetToolsAsync();
            AvailableTools.Clear();
            foreach (var availableTool in availableTools)
                AvailableTools.Add(availableTool.Name);
            
            // get tools for agent
            if (_agent.Id != null)
            {
                var agentTools = await _configurationService.GetToolsForAgentAsync(_agent.Id.Value);
                
                FormModel.Tools.Clear();
                foreach (var agentTool in agentTools)
                    FormModel.Tools.Add(agentTool.Name);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading agents: {ex.Message}");
        }
        
        // TODO actually load - sources will determine the models
        AvailableChatModels.Clear();
        AvailableChatModels.Add("gpt-4.1-2025-04-14");
        AvailableChatModels.Add("qwen3:32b-q4_K_M");
        AvailableChatModels.Add("cogito:32b-v1-preview-qwen-q4_K_M");
        AvailableEmbeddingModels.Clear();
        AvailableEmbeddingModels.Add("text-embedding-3-large");
        AvailableEmbeddingModels.Add("mxbai-embed-large:latest");
        AvailableVisionModels.Clear();
        AvailableVisionModels.Add("gpt-4.1-2025-04-14");
        AvailableVisionModels.Add("gemma3:12b");
    }

    /// <summary>
    /// Executes the logic to save the changes made in the form.
    /// This method validates the form model before saving the data.
    /// If validation is successful, the method applies the changes from the form model to the underlying agent object,
    /// persists the data, and displays a success notification to the user.
    /// The method also initiates the closure of the associated dialog or view.
    /// </summary>
    private void ExecuteSaveCommand()
    {
        if (FormModel.Validate())
        {
            // TODO - apply FormModel to AesirAgentBase, store, store tool selection
            _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' updated");
            Close();
        }
    }

    /// <summary>
    /// Executes the cancel operation for the current view model.
    /// </summary>
    /// <remarks>
    /// This method is typically invoked when the user opts to discard any pending changes
    /// to the form or data. It triggers the closure of the associated view or dialog without
    /// saving any modifications.
    /// </remarks>
    private void ExecuteCancelCommand()
    {
        Close();
    }

    /// <summary>
    /// Executes a command to delete the current form model associated with the view model,
    /// triggers a success notification upon completion, and closes the dialog.
    /// </summary>
    private void ExecuteDeleteCommand()
    {
        // TODO - delete, toast?
        _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' deleted");
        Close();
    }

    /// <summary>
    /// Triggers the request to close the associated view or dialog by invoking the <see cref="RequestClose"/> event.
    /// </summary>
    /// <remarks>
    /// This method is typically invoked internally to signal that the current operation
    /// or interaction should conclude, and the related view or dialog should be closed.
    /// </remarks>
    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }
}

/// <summary>
/// Represents a model for the agent form data used to bind properties and validate inputs in the agent view.
/// </summary>
public partial class AgentFormDataModel : ObservableValidator
{
    /// <summary>
    /// Represents the name of the agent, required for validation and user input.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Name is required")] private string? _name;


    /// <summary>
    /// Represents the source of the model, specifying its origin or provider.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Source is required")] private ModelSource? _source;


    /// <summary>
    /// Represents the current prompt context for the agent, which is required for processing.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Prompt is required")] private PromptPersona? _prompt;


    /// <summary>
    /// Represents the selected chat model for the agent, which is a required field and is validated for compliance.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Chat Model is required")] private string? _chatModel;


    /// <summary>
    /// The embedding model represented as a string, required for setting or validating the embedding configuration.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Embedding Model is required")] private string? _embeddingModel;


    /// <summary>
    /// Represents the vision model input, required for agent form data validation.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Vision Model is required")] private string? _visionModel;

    /// <summary>
    /// Collection of tools used within the AgentFormDataModel
    /// </summary>
    [ObservableProperty] private ObservableCollection<string> _tools = new ObservableCollection<string>();

    /// <summary>
    /// Validates all properties of the current object and checks if any validation errors exist.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the current object has no validation errors.
    /// Returns true if the object is valid, otherwise false.
    /// </returns>
    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }
}