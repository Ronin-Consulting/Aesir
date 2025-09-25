using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.Validators;
using Aesir.Client.Views;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Ursa.Controls;

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
    /// Service for accessing models.
    /// </summary>
    private readonly IModelService _modelService;
    
    /// <summary>
    /// Represents the initially selected inference engine for chat models
    /// </summary>
    private Guid? _initialChatInferenceEngineId;

    /// <summary>
    /// Represents the form data model for the agent view, used to handle data
    /// binding and validation within the AgentViewViewModel.
    /// </summary>
    [ObservableProperty] private AgentFormDataModel _formModel;

    /// <summary>
    /// Collection of available prompt personas that defines the context in which the agent operates.
    /// </summary>
    public ObservableCollection<PromptPersona> AvailableChatPromptPersonas { get; } = new(Enum.GetValues<PromptPersona>());

    /// <summary>
    /// Collection of available inference engines for chat models
    /// </summary>
    public ObservableCollection<AesirInferenceEngineBase> AvailableChatInferenceEngines { get; set; }
    
    /// <summary>
    /// Collection of available chat models that can be used.
    /// </summary>
    public ObservableCollection<AesirModelInfo> AvailableChatModels { get; }

    /// <summary>
    /// Collection of tools available for selection in the agent configuration.
    /// </summary>
    public ObservableCollection<AesirToolBase> AvailableTools { get; set; }

    /// <summary>
    /// Indicates whether the view model has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Command executed to edit a custom prompt content.
    /// </summary>
    public ICommand EditCustomPromptCommand { get; set; }

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

    public ICommand ShowModelDetailsCommand { get; set; }
    
    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;

    /// Represents the view model for the agent view. Handles the binding of agent data
    /// and communication between the user interface and underlying services.
    public AgentViewViewModel(AesirAgentBase agent, 
            INotificationService notificationService,
            IConfigurationService configurationService,
            IModelService modelService)
    {
        _agent = agent;
        _notificationService = notificationService;
        _configurationService = configurationService;
        _modelService = modelService;

        _initialChatInferenceEngineId = agent.ChatInferenceEngineId;
        
        FormModel = new AgentFormDataModel
        {
            IsExisting = agent.Id.HasValue,
            Name = agent.Name,
            Description = agent.Description,
            ChatPromptPersona = agent.ChatPromptPersona,
            ChatCustomPromptContent = agent.ChatCustomPromptContent,
            ChatModel = new AesirModelInfo()
            {
                Id = agent.ChatModel!
            },
            ChatMaxTokens = agent.ChatMaxTokens,
            ChatTemperature = agent.ChatTemperature,
            ChatTopP = agent.ChatTopP,
            Tools = []
        };
        IsDirty = false;
        EditCustomPromptCommand = new RelayCommand(ExecuteEditCustomPrompt);
        SaveCommand = new AsyncRelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommand);
        ShowModelDetailsCommand = new AsyncRelayCommand(ExecuteShowModelDetailsCommand);
        
        AvailableChatInferenceEngines = [];
        AvailableChatModels = [];
        AvailableTools = [];
    
        FormModel.PropertyChanged += OnFormModelPropertyChanged;
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
    /// Clears and repopulates available collections for chat models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of loading data.</returns>
    private async Task LoadAvailableAsync()
    {
        try
        {
            // first get available engines
            var availableInferenceEngines = await _configurationService.GetInferenceEnginesAsync();
            AvailableChatInferenceEngines.Clear();
            foreach (var availableInferenceEngine in availableInferenceEngines)
                AvailableChatInferenceEngines.Add(availableInferenceEngine);
            
            // disable property change as we are about to set some values that cause loading
            FormModel.PropertyChanged -= OnFormModelPropertyChanged;
            
            // establish the selected inference engines
            if (_initialChatInferenceEngineId != null)
                FormModel.ChatInferenceEngine = AvailableChatInferenceEngines.First(s => s.Id == _initialChatInferenceEngineId);

            // re-enable property change
            FormModel.PropertyChanged += OnFormModelPropertyChanged;
            
            // get available models if available
            await LoadChatModels();

            // get available tools
            var availableTools = await _configurationService.GetToolsAsync();
            AvailableTools.Clear();
            foreach (var availableTool in availableTools)
                AvailableTools.Add(availableTool);
            
            // get tools for agent
            if (_agent.Id != null)
            {
                var agentTools = await _configurationService.GetToolsForAgentAsync(_agent.Id.Value);
                
                FormModel.Tools.Clear();
                foreach (var agentTool in agentTools)
                    FormModel.Tools.Add(agentTool);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading agents: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads chat models
    /// </summary>
    private async Task LoadChatModels()
    {
        AvailableChatModels.Clear();
        
        if (FormModel.ChatInferenceEngine == null)
            return;

        try
        {
            var availableModels = await _modelService.GetModelsAsync(
                FormModel.ChatInferenceEngine.Id!.Value, ModelCategory.Chat);
            foreach (var model in availableModels)
                AvailableChatModels.Add(model);
        }
        catch (Exception e)
        {
            // TODO
        }
        
        // The SelectedItem wasn't in the ComboBox's collection initially since we just loaded it. Ideally it
        // would re-evaluate, but it doesn't want to. There is a very talked about issue with this timing.
        // We have to force the ComboBox to re-bind the SelectedItem to get it to work.
        
        var currentChatModel = FormModel.ChatModel;
        
        FormModel.ChatModel = null;

        // Be a little wiser and only set the value if they exist in the collections (may have been removed from db)
        if (currentChatModel != null && AvailableChatModels.Contains(currentChatModel))
            // re-load the chat model from the collection because it can have full details and not just id.
            FormModel.ChatModel = AvailableChatModels.First(m => m.Id == currentChatModel.Id);
    }

    /// <summary>
    /// Handles property changes in the form model to trigger related updates.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data containing the name of the property that changed.</param>
    private void OnFormModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {       
        if (e.PropertyName == nameof(AgentFormDataModel.ChatInferenceEngine))
        {
            _ = LoadChatModels();
        }
    }

    /// <summary>
    /// Executes the logic to edit custom prompt content.
    /// </summary>
    private void ExecuteEditCustomPrompt()
    {
        // TODO
    }

    /// <summary>
    /// Executes the logic to save the changes made in the form.
    /// This method validates the form model before saving the data.
    /// If validation is successful, the method applies the changes from the form model to the underlying agent object,
    /// persists the data, and displays a success notification to the user.
    /// The method also initiates the closure of the associated dialog or view.
    /// </summary>
    private async Task ExecuteSaveCommand()
    {
        if (FormModel.Validate())
        {
            var agent = new AesirAgentBase()
            {
                Name = FormModel.Name,
                Description = FormModel.Description,
                ChatInferenceEngineId = FormModel.ChatInferenceEngine!.Id,
                ChatModel = FormModel.ChatModel!.Id,
                ChatPromptPersona = FormModel.ChatPromptPersona,
                ChatCustomPromptContent = FormModel.ChatPromptPersona == PromptPersona.Custom ? FormModel.ChatCustomPromptContent : null,
                ChatMaxTokens = FormModel.ChatMaxTokens,
                ChatTemperature = FormModel.ChatTemperature,
                ChatTopP = FormModel.ChatTopP
            };

            var closeResult = CloseResult.Errored;
            
            try
            {
                var selectedTools = FormModel.Tools.Select(t => t.Id.Value).ToArray();
                
                if (_agent.Id == null)
                {
                    var id = await _configurationService.CreateAgentAsync(agent);

                    await _configurationService.UpdateToolsForAgentAsync(id, selectedTools);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' created");

                    closeResult = CloseResult.Created;
                }
                else
                {
                    agent.Id = _agent.Id;
                    await _configurationService.UpdateAgentAsync(agent);

                    await _configurationService.UpdateToolsForAgentAsync(agent.Id.Value, selectedTools);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' updated");
                    
                    closeResult = CloseResult.Updated;
                }
            }
            catch (Exception e)
            {   
                _notificationService.ShowErrorNotification("Error",
                    $"'{FormModel.Name}' failed to save with: {e.Message}");

                Console.WriteLine(e);
            }
            finally
            {
                Close(closeResult);
            }
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
        Close(CloseResult.Cancelled);
    }

    /// <summary>
    /// Executes a command to delete the current form model associated with the view model,
    /// triggers a success notification upon completion, and closes the dialog.
    /// </summary>
    private async Task ExecuteDeleteCommand()
    {
        var closeResult = CloseResult.Errored;
        
        try
        {
            if (_agent.Id != null)
            {
                await _configurationService.DeleteAgentAsync(_agent.Id.Value);

                _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' deleted");

                closeResult = CloseResult.Deleted;
            }
        }
        catch (Exception e)
        {
            _notificationService.ShowErrorNotification("Error",
                $"'{FormModel.Name}' failed to delete with: {e.Message}");

            Console.WriteLine(e);
        }
        finally
        {
            Close(closeResult);
        }
    }

    private async Task ExecuteShowModelDetailsCommand()
    {
        var options = new OverlayDialogOptions()
        {
            FullScreen = false,
            HorizontalAnchor = HorizontalPosition.Center,
            VerticalAnchor = VerticalPosition.Center,
            Mode = DialogMode.Info,
            Buttons = DialogButton.OK,
            Title = "Model Details",
            CanLightDismiss = true,
            CanDragMove = true,
            IsCloseButtonVisible = true,
            CanResize = false
        };

        var modelDetail = FormModel.ChatModel;

        await OverlayDialog.ShowModal<ModelDetailView, ModelDetailViewViewModel>(
            new ModelDetailViewViewModel(modelDetail?.Details), options: options);
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
        Close(CloseResult.Cancelled);
    }

    /// <summary>
    /// Triggers the request to close the associated view or dialog by invoking the <see cref="RequestClose"/> event.
    /// </summary>
    /// <param name="closeResult">The results to send to the close event</param>
    private void Close(CloseResult closeResult)
    {
        RequestClose?.Invoke(this, closeResult);
    }
    
    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    
        FormModel.PropertyChanged -= OnFormModelPropertyChanged;
    }
}

/// <summary>
/// Represents a model for the agent form data used to bind properties and validate inputs in the agent view.
/// </summary>
public partial class AgentFormDataModel : ObservableValidator
{
    /// <summary>
    /// Represents if the agent is new or existing
    /// </summary>
    [ObservableProperty] private bool? _isExisting;
    
    /// <summary>
    /// Represents the name of the agent, required for validation and user input.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Name is required")] private string? _name;

    /// <summary>
    /// Represents the current prompt context for the agent, which is required for processing.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Prompt is required")] private PromptPersona? _chatPromptPersona;

    /// <summary>
    /// Represents the current prompt content for the agent when the prompt persona is custom
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [ConditionalRequired(nameof(ChatPromptPersona), Aesir.Common.Prompts.PromptPersona.Custom, ErrorMessage = "Prompt content is required")] 
    private string? _chatCustomPromptContent;
    
    /// <summary>
    /// Represents the specific Inference Engine selected for chat
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Chat Inference Engine is required")] private AesirInferenceEngineBase? _chatInferenceEngine;

    /// <summary>
    /// Represents the selected chat model for the agent, which is a required field and is validated for compliance.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Chat Model is required")] private AesirModelInfo? _chatModel;
    
    /// <summary>
    /// Represents the max tokens allowed for the chat session.
    /// </summary>
    [ObservableProperty] private int? _chatMaxTokens;

    /// <summary>
    /// Represents the temperature model hyperparameter for the chat session.
    /// </summary>
    [ObservableProperty] private double? _chatTemperature;

    /// <summary>
    /// Represents the top p model hyperparameter for the chat session.
    /// </summary>
    [ObservableProperty] private double? _chatTopP;

    /// <summary>
    /// Collection of tools used within the AgentFormDataModel
    /// </summary>
    [ObservableProperty] private ObservableCollection<AesirToolBase> _tools = [];
    
    /// <summary>
    /// Represents the description of the agent, required for validation and user input.
    /// </summary>
    [ObservableProperty] private string? _description;

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

    /// Clears the validation errors for a specific property or all properties if no property name is provided.
    /// <param name="propertyName">
    /// The name of the property whose validation errors should be cleared.
    /// If null, validation errors for all properties will be cleared.
    /// </param>
    public void ClearValidation(string? propertyName = null)
    {
        ClearErrors(propertyName);
    }
}