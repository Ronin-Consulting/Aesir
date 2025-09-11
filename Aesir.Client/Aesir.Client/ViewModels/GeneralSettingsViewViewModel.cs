using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for general settings related views, providing properties, commands,
/// and events to manage inference engine configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> and implements <see cref="IDialogContext"/>,
/// enabling it to work with MVVM patterns and dialog-based user interactions. It manages
/// collections of available models, tools, and prompts, as well as command bindings for
/// managing general settings configuration lifecycle actions such as save, cancel, and delete.
/// </remarks>
/// <example>
/// Typically used in the context of general settings detail views within the application. The view model
/// is activated and bound to the associated user control for displaying and managing general settings
/// data.
/// </example>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IDialogContext" />
public partial class GeneralSettingsViewViewModel : ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Represents the underlying general settings configuration and details used by the view model,
    /// including properties such as ID, name, type, description, and configuration.
    /// </summary>
    private AesirGeneralSettingsBase _generalSettings;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including tools
    /// and general settings, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Service for accessing models.
    /// </summary>
    private readonly IModelService _modelService;

    /// <summary>
    /// Represents the form data model for the general settings view, used to handle data
    /// binding and validation within the AgentViewViewModel.
    /// </summary>
    [ObservableProperty] private GeneralSettingsFormDataModel _formModel;

    /// <summary>
    /// Collection of available engine type that defines how the general settings is accessed
    /// </summary>
    public ObservableCollection<AesirInferenceEngineBase> AvailableInferenceEngines { get; set; }

    /// <summary>
    /// Collection of available rad embedding models
    /// </summary>
    public ObservableCollection<string> AvailableRagEmbeddingModels { get; set; }
    
    /// <summary>
    /// Collection of available rad vision models
    /// </summary>
    public ObservableCollection<string> AvailableRagVisionModels { get; set; }

    /// <summary>
    /// Indicates whether the view model has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Command executed to save the current state or data of the general settings view.
    /// </summary>
    public ICommand SaveCommand { get; set; }

    /// <summary>
    /// Command used to cancel the current operation or revert changes in the General Settings View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;

    /// Represents the view model for the general settings view. Handles the binding of data
    /// and communication between the user interface and underlying services.
    public GeneralSettingsViewViewModel(INotificationService notificationService,
            IConfigurationService configurationService,
                IModelService modelService)
    {
        _notificationService = notificationService;
        _configurationService = configurationService;
        _modelService = modelService;
        
        FormModel = new();
        
        IsDirty = false;
        SaveCommand = new AsyncRelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        
        AvailableInferenceEngines = new ObservableCollection<AesirInferenceEngineBase>();
        AvailableRagEmbeddingModels = new ObservableCollection<string>();
        AvailableRagVisionModels = new ObservableCollection<string>();
    
        FormModel.PropertyChanged += OnFormModelPropertyChanged;
    }

    /// <summary>
    /// Invoked when the view model is activated. This method executes initialization logic such as
    /// invoking UI-thread-specific tasks and loading the necessary resources or state for operation.
    /// </summary>
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadSettingsAsync);
    }

    /// <summary>
    /// Asynchronously loads and initializes available tools, model sources, and models for the general settings.
    /// This method retrieves available tools through the configuration service, updates relevant observable collections
    /// with the data, and configures the tools associated with the current general setting.
    /// Clears and repopulates available collections for chat models, and vision models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of loading data.</returns>
    private async Task LoadSettingsAsync()
    {
        try
        {
            // get available engines and settings
            var availableInferenceEnginesTask = _configurationService.GetInferenceEnginesAsync();
            var generalSettingsTask = _configurationService.GetGeneralSettingsAsync();
            await Task.WhenAll(availableInferenceEnginesTask, generalSettingsTask);
            
            var generalSettings = await generalSettingsTask;
            var availableInferenceEngines = await availableInferenceEnginesTask;
            AvailableInferenceEngines.Clear();
            foreach (var availableInferenceEngine in availableInferenceEngines)
                AvailableInferenceEngines.Add(availableInferenceEngine);

            // disable property change as we are about to set some values that cause loading
            FormModel.PropertyChanged -= OnFormModelPropertyChanged;
            
            if (generalSettings.RagEmbeddingInferenceEngineId != null)
                FormModel.RagEmbeddingInferenceEngine = AvailableInferenceEngines
                    .First(s => s.Id == generalSettings.RagEmbeddingInferenceEngineId);
            FormModel.RagEmbeddingModel = generalSettings.RagEmbeddingModel ?? "";
            if (generalSettings.RagVisionInferenceEngineId != null)
                FormModel.RagVisionInferenceEngine = AvailableInferenceEngines
                    .First(s => s.Id == generalSettings.RagVisionInferenceEngineId);
            FormModel.RagVisionModel = generalSettings.RagVisionModel ?? "";
            FormModel.TtsModelPath = generalSettings.TtsModelPath ?? "";
            FormModel.SttModelPath = generalSettings.SttModelPath ?? "";
            FormModel.VadModelPath = generalSettings.VadModelPath ?? "";

            // re-enable property change
            FormModel.PropertyChanged += OnFormModelPropertyChanged;
            
            // get available models if available
            await LoadRagEmbeddingModels();
            await LoadRagVisionModels();

            FormModel.ClearValidation();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading general settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads RAG embedding models
    /// </summary>
    private async Task LoadRagEmbeddingModels()
    {
        AvailableRagEmbeddingModels.Clear();
        
        if (FormModel.RagEmbeddingInferenceEngine == null)
            return;

        try
        {
            var availableModels = await _modelService.GetModelsAsync(
                FormModel.RagEmbeddingInferenceEngine.Id.Value, ModelCategory.Embedding);
            foreach (var model in availableModels)
                AvailableRagEmbeddingModels.Add(model.Id);
        }
        catch (Exception e)
        {
            // TODO
        }
        
        // The SelectedItem wasn't in the ComboBox's collection initially since we just loaded it. Ideally it
        // would re-evaluate, but it doesn't want to. There is a very talked about issue with this timing.
        // We have to force the ComboBox to re-bind the SelectedItem to get it to work.
        
        var currentChatModel = FormModel.RagEmbeddingModel;
        
        FormModel.RagEmbeddingModel = null;

        // Be a little wiser and only set the value if they exist in the collections (may have been removed from db)
        if (!string.IsNullOrEmpty(currentChatModel) && AvailableRagEmbeddingModels.Contains(currentChatModel))
            FormModel.RagEmbeddingModel = currentChatModel;
        
        FormModel.ClearValidation(nameof(FormModel.RagEmbeddingModel));
    }

    /// <summary>
    /// Loads RAG vision models
    /// </summary>
    private async Task LoadRagVisionModels()
    {
        AvailableRagVisionModels.Clear();
        
        if (FormModel.RagVisionInferenceEngine == null)
            return;

        try
        {
            var availableModels = await _modelService.GetModelsAsync(
                FormModel.RagVisionInferenceEngine.Id.Value, ModelCategory.Vision);
            foreach (var model in availableModels)
                AvailableRagVisionModels.Add(model.Id);
        }
        catch (Exception e)
        {
            // TODO
        }
        
        // The SelectedItem wasn't in the ComboBox's collection initially since we just loaded it. Ideally it
        // would re-evaluate, but it doesn't want to. There is a very talked about issue with this timing.
        // We have to force the ComboBox to re-bind the SelectedItem to get it to work.
        
        var currentChatModel = FormModel.RagVisionModel;
        
        FormModel.RagVisionModel = null;

        // Be a little wiser and only set the value if they exist in the collections (may have been removed from db)
        if (!string.IsNullOrEmpty(currentChatModel) && AvailableRagVisionModels.Contains(currentChatModel))
            FormModel.RagVisionModel = currentChatModel;
        
        FormModel.ClearValidation(nameof(FormModel.RagVisionModel));
    }

    /// <summary>
    /// Handles property changes in the form model to trigger related updates.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data containing the name of the property that changed.</param>
    private void OnFormModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettingsFormDataModel.RagEmbeddingInferenceEngine))
        {
            _ = LoadRagEmbeddingModels();
        }
        if (e.PropertyName == nameof(GeneralSettingsFormDataModel.RagVisionInferenceEngine))
        {
            _ = LoadRagVisionModels();
        }
    }

    /// <summary>
    /// Executes the logic to save the changes made in the form.
    /// This method validates the form model before saving the data.
    /// If validation is successful, the method applies the changes from the form model to the underlying general
    /// settings object, persists the data, and displays a success notification to the user.
    /// The method also initiates the closure of the associated dialog or view.
    /// </summary>
    private async Task ExecuteSaveCommand()
    {
        if (FormModel.Validate())
        {
            var generalSettings = new AesirGeneralSettingsBase()
            {
                RagEmbeddingInferenceEngineId = FormModel.RagEmbeddingInferenceEngine.Id,
                RagEmbeddingModel = FormModel.RagEmbeddingModel,
                RagVisionInferenceEngineId = FormModel.RagVisionInferenceEngine.Id,
                RagVisionModel = FormModel.RagVisionModel,
                TtsModelPath = FormModel.TtsModelPath,
                SttModelPath = FormModel.SttModelPath,
                VadModelPath = FormModel.VadModelPath
            };

            var closeResult = CloseResult.Errored;
            
            try
            {
                await _configurationService.UpdateGeneralSettingsAsync(generalSettings);

                _notificationService.ShowSuccessNotification("Success", $"General Settings updated");
                
                closeResult = CloseResult.Updated;
            }
            catch (Exception e)
            {   
                _notificationService.ShowErrorNotification("Error",
                    $"General Settings failed to save with: {e.Message}");

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
/// Represents a model for the general settings form data used to bind properties and validate inputs in the
/// general settings view.
/// </summary>
public partial class GeneralSettingsFormDataModel : ObservableValidator
{   
    /// <summary>
    /// Represents the inference engine for RAG, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "RAG Embedding Inference Engine is required")] 
    private AesirInferenceEngineBase? _ragEmbeddingInferenceEngine;
    
    /// <summary>
    /// Represents the name of the embedding model for RAG, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "RAG Embedding Model is required")] 
    private string? _ragEmbeddingModel;
    
    /// <summary>
    /// Represents the inference engine for RAG, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "RAG Vision Inference Engine is required")] 
    private AesirInferenceEngineBase? _ragVisionInferenceEngine;
    
    /// <summary>
    /// Represents the name of the vision model for RAG, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "RAG Vision Model is required")] 
    private string? _ragVisionModel;
    
    /// <summary>
    /// Represents the TTS model path, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "Text-to-Speech Model is required")] 
    private string? _ttsModelPath;
    
    /// <summary>
    /// Represents the STT model path, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "Speech-to-Text Model is required")] 
    private string? _sttModelPath;
    
    /// <summary>
    /// Represents the VAD model path, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "Voice Activity Detection Model is required")] 
    private string? _vadModelPath;

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