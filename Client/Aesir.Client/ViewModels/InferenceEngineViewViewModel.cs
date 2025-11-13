using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.Validators;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Newtonsoft.Json;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for inference engine-related views, providing properties, commands,
/// and events to manage inference engine configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> and implements <see cref="IDialogContext"/>,
/// enabling it to work with MVVM patterns and dialog-based user interactions. It manages
/// collections of available models, tools, and prompts, as well as command bindings for
/// managing inference engine configuration lifecycle actions such as save, cancel, and delete.
/// </remarks>
/// <example>
/// Typically used in the context of inference engine detail views within the application. The view model
/// is activated and bound to the associated user control for displaying and managing inference engine
/// data.
/// </example>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IDialogContext" />
public partial class InferenceEngineViewViewModel : ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Represents the underlying inference engine configuration and details used by the view model,
    /// including properties such as ID, name, type, description, and configuration.
    /// </summary>
    private AesirInferenceEngineBase _inferenceEngine;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including tools
    /// and inference engines, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Represents the form data model for the inference engine view, used to handle data
    /// binding and validation within the AgentViewViewModel.
    /// </summary>
    [ObservableProperty] private InferenceEngineFormDataModel _formModel;

    /// <summary>
    /// Collection of available engine type that defines how the inference engine is accessed
    /// </summary>
    public ObservableCollection<InferenceEngineType> AvailableEngineTypes { get; } = new(Enum.GetValues<InferenceEngineType>());

    /// <summary>
    /// Indicates whether the view model has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Command executed to save the current state or data of the inference engine view.
    /// </summary>
    public ICommand SaveCommand { get; set; }

    /// <summary>
    /// Command used to cancel the current operation or revert changes in the Inference Engine View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Command used to delete the associated inference engine or entity.
    /// </summary>
    public ICommand DeleteCommand { get; set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;

    /// Represents the view model for the inference engine view. Handles the binding of data
    /// and communication between the user interface and underlying services.
    public InferenceEngineViewViewModel(AesirInferenceEngineBase inferenceEngine, 
            INotificationService notificationService,
            IConfigurationService configurationService)
    {
        _inferenceEngine = inferenceEngine;
        _notificationService = notificationService;
        _configurationService = configurationService;
        
        FormModel = new()
        {
            IsExisting = inferenceEngine.Id.HasValue,
            Name = inferenceEngine.Name,
            Description = inferenceEngine.Description,
            EngineType = inferenceEngine.Type,
            Configuration = JsonConvert.SerializeObject(inferenceEngine.Configuration ?? new Dictionary<string, string?>(), Formatting.Indented)
        };
        
        IsDirty = false;
        SaveCommand = new AsyncRelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommand);
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
    /// Asynchronously loads and initializes available tools, model sources, and models for the inference engine.
    /// This method retrieves available tools through the configuration service, updates relevant observable collections
    /// with the data, and configures the tools associated with the current inference engine.
    /// Clears and repopulates available collections for chat models, and vision models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of loading data.</returns>
    private async Task LoadAvailableAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes the logic to save the changes made in the form.
    /// This method validates the form model before saving the data.
    /// If validation is successful, the method applies the changes from the form model to the underlying inference
    /// engine object, persists the data, and displays a success notification to the user.
    /// The method also initiates the closure of the associated dialog or view.
    /// </summary>
    private async Task ExecuteSaveCommand()
    {
        if (FormModel.Validate())
        {
            var inferenceEngine = new AesirInferenceEngineBase()
            {
                Name = FormModel.Name,
                Description = FormModel.Description,
                Type = FormModel.EngineType,
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, string?>>(FormModel.Configuration ?? string.Empty) 
                                ?? new Dictionary<string, string?>()
            };

            var closeResult = CloseResult.Errored;
            
            try
            {
                if (_inferenceEngine.Id == null)
                {
                    await _configurationService.CreateInferenceEngineAsync(inferenceEngine);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' created");

                    closeResult = CloseResult.Created;
                }
                else
                {
                    inferenceEngine.Id = _inferenceEngine.Id;
                    await _configurationService.UpdateInferenceEngineAsync(inferenceEngine);

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
            if (_inferenceEngine.Id != null)
            {
                await _configurationService.DeleteInferenceEngineAsync(_inferenceEngine.Id.Value);

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
}

/// <summary>
/// Represents a model for the inference engine form data used to bind properties and validate inputs in the inference engine view.
/// </summary>
public partial class InferenceEngineFormDataModel : ObservableValidator
{
    /// <summary>
    /// Represents if the inference engine is new or existing
    /// </summary>
    [ObservableProperty] 
    private bool? _isExisting;
    
    /// <summary>
    /// Represents the name of the inference engine, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "Name is required")] 
    private string? _name;
    
    /// <summary>
    /// Represents the description of the inference engine, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    private string? _description;

    /// <summary>
    /// Represents the current engine type for the inference engine
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "Type is required")] 
    private InferenceEngineType? _engineType;
    
    /// <summary>
    /// Represents the description of the inference engine, required for validation and user input.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [Required (ErrorMessage = "Configuration is required")] 
    [ValidJsonDictionary(ErrorMessage = "Configuration must be valid JSON")]
    private string? _configuration;

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