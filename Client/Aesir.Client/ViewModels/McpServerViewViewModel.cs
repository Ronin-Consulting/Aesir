using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.Validators;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for MCP Server-related views, providing properties, commands,
/// and events to manage MCP Server configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> and implements <see cref="IDialogContext"/>,
/// enabling it to work with MVVM patterns and dialog-based user interactions. It manages
/// command bindings for managing MCP Server configuration lifecycle actions such as save, cancel, and delete.
/// </remarks>
/// <example>
/// Typically used in the context of MCP Server detail views within the application. The view model
/// is activated and bound to the associated user control for displaying and managing MCP Server
/// data.
/// </example>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IDialogContext" />
public partial class McpServerViewViewModel : ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Represents the underlying MCP Server configuration and details used by the view model, including properties such as ID, name, and type.
    /// </summary>
    private readonly AesirMcpServerBase _mcpServer;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Provides an instance of the dialog service used to encapsulate and manage dialog-related interactions
    /// within the view model, such as opening, closing, and configuring dialogs.
    /// </summary>
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Service for accessing and managing configuration data, including MCP Servers
    /// and MCP Servers, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Collection of available tool types that can be selected or used within the application.
    /// </summary>
    public ObservableCollection<ServerLocation> AvailableLocations { get; } = new(Enum.GetValues<ServerLocation>());

    /// <summary>
    /// Represents the form data model for the MCP Server view, used to handle data
    /// binding and validation within the McpServerViewViewModel.
    /// </summary>
    [ObservableProperty] private McpServerFormDataModel _formModel;

    /// <summary>
    /// Indicates whether the view model has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }
    
    /// <summary>
    /// Command executed to delete an argument
    /// </summary>
    public ICommand DeleteArgumentCommand { get; set; }
    
    /// <summary>
    /// Command executed to add an argument
    /// </summary>
    public ICommand AddArgumentCommand { get; set; }
    
    /// <summary>
    /// Command executed to delete an environment variable
    /// </summary>
    public ICommand DeleteEnvironmentVariableCommand { get; set; }
    
    /// <summary>
    /// Command executed to add an environment variable
    /// </summary>
    public ICommand AddEnvironmentVariableCommand { get; set; }
    
    /// <summary>
    /// Command executed to delete an HTTP Header 
    /// </summary>
    public ICommand DeleteHttpHeaderCommand { get; set; }
    
    /// <summary>
    /// Command executed to add an HTTP Header
    /// </summary>
    public ICommand AddHttpHeaderCommand { get; set; }

    /// <summary>
    /// Command executed to save the current state or data of the MCP Server view.
    /// </summary>
    public ICommand SaveCommand { get; set; }

    /// <summary>
    /// Command used to cancel the current operation or revert changes in the MCP Server View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Command used to delete the associated MCP Server or entity.
    /// </summary>
    public ICommand DeleteCommand { get; set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;

    /// Represents the view model for the MCP Server view. Handles the binding of MCP Server data
    /// and communication between the user interface and underlying services.
    public McpServerViewViewModel(AesirMcpServerBase mcpServer, 
            INotificationService notificationService,
            IDialogService dialogService,
            IConfigurationService configurationService)
    {
        _mcpServer = mcpServer;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _configurationService = configurationService;
        
        FormModel = new McpServerFormDataModel
        {
            IsExisting = mcpServer.Id.HasValue,
            Name = mcpServer.Name,
            Description = mcpServer.Description,
            Location = mcpServer.Location,
            Command = mcpServer.Command,
            Url = mcpServer.Url
        };
        switch (mcpServer.Location)
        {
            case ServerLocation.Local:
            {
                foreach (var argument in mcpServer.Arguments)
                    FormModel.Arguments.Add(new ArgumentItem { Value = argument });
                foreach (var environmentVariable in mcpServer.EnvironmentVariables)
                    FormModel.EnvironmentVariables.Add(new EnvironmentVariableItem()
                        { Name = environmentVariable.Key, Value = environmentVariable.Value ?? "" });
                break;
            }
            case ServerLocation.Remote:
            {
                foreach (var httpHeader in mcpServer.HttpHeaders)
                    FormModel.HttpHeaders.Add(
                        new HttpHeaderItem() { Name = httpHeader.Key, Value = httpHeader.Value ?? "" });
                break;
            }
            case null:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        IsDirty = false;
        SaveCommand = new AsyncRelayCommand(ExecuteSaveCommandAsync);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommandAsync);
        DeleteArgumentCommand = new RelayCommand<ArgumentItem>(ExecuteDeleteArgumentCommand);
        AddArgumentCommand = new RelayCommand(ExecuteAddArgumentCommand);
        DeleteEnvironmentVariableCommand = new RelayCommand<EnvironmentVariableItem>(ExecuteDeleteEnvironmentVariableCommand);
        AddEnvironmentVariableCommand = new RelayCommand(ExecuteAddEnvironmentVariableCommand);
        DeleteHttpHeaderCommand = new RelayCommand<HttpHeaderItem>(ExecuteDeleteHttpHeaderCommand);
        AddHttpHeaderCommand = new RelayCommand(ExecuteAddHttpHeaderCommand);
    }

    /// Removes the specified argument from the list of arguments in the form model.
    /// Ensures the argument exists in the list before attempting to remove it.
    /// <param name="argument">The argument to be deleted. If null or not found in the list, no operation is performed.</param>
    private void ExecuteDeleteArgumentCommand(ArgumentItem? argument)
    {
        if (argument != null && FormModel.Arguments.Contains(argument))
        {
            FormModel.Arguments.Remove(argument);
        }
    }

    /// Adds a new argument to the form model's argument collection. The newly added
    /// argument initializes with an empty value. Typically used to allow users to
    /// dynamically add more arguments to the MCP Server configuration.
    private void ExecuteAddArgumentCommand()
    {
        FormModel.Arguments.Add(new ArgumentItem { Value = "" });
    }

    /// Removes the specified environment variable from the collection of environment variables
    /// in the form model, if it exists.
    /// <param name="environmentVariable">The environment variable to be removed. If null or not present
    /// in the collection, the method performs no operation.</param>
    private void ExecuteDeleteEnvironmentVariableCommand(EnvironmentVariableItem? environmentVariable)
    {
        if (environmentVariable != null && FormModel.EnvironmentVariables.Contains(environmentVariable))
        {
            FormModel.EnvironmentVariables.Remove(environmentVariable);
        }
    }

    /// Adds a new environment variable to the form model with empty name and value properties.
    /// This command modifies the collection of environment variables bound to the user interface.
    private void ExecuteAddEnvironmentVariableCommand()
    {
        FormModel.EnvironmentVariables.Add(new EnvironmentVariableItem() { Name = "", Value = "" });
    }

    /// Executes the command to delete a specified HTTP header from the current form model's list of HTTP headers.
    /// <param name="httpHeader">
    /// The HTTP header item to be removed. If null or not present in the list, no action is performed.
    /// </param>
    private void ExecuteDeleteHttpHeaderCommand(HttpHeaderItem? httpHeader)
    {
        if (httpHeader != null && FormModel.HttpHeaders.Contains(httpHeader))
        {
            FormModel.HttpHeaders.Remove(httpHeader);
        }
    }

    /// Handles the execution of the command to add a new HTTP header to the form model.
    /// This method appends an empty HTTP header entry to the HttpHeaders collection,
    /// allowing users to define a new header name and value within the user interface.
    private void ExecuteAddHttpHeaderCommand()
    {
        FormModel.HttpHeaders.Add(new HttpHeaderItem() { Name = "", Value = "" });
    }

    /// <summary>
    /// Executes the logic to save the changes made in the form.
    /// This method validates the form model before saving the data.
    /// If validation is successful, the method applies the changes from the form model to the underlying MCP Server object,
    /// persists the data, and displays a success notification to the user.
    /// The method also initiates the closure of the associated dialog or view.
    /// </summary>
    private async Task ExecuteSaveCommandAsync()
    {
        if (FormModel.Validate())
        {
            var mcpServer = new AesirMcpServerBase()
            {
                Name = FormModel.Name,
                Description = FormModel.Description,
                Location = FormModel.Location,
                Arguments = new List<string>(),
                EnvironmentVariables = new Dictionary<string, string?>(),
                HttpHeaders = new Dictionary<string, string?>()
            };

            if (FormModel.Location == ServerLocation.Local)
            {
                mcpServer.Command = FormModel.Command;
                mcpServer.Arguments = FormModel.Arguments
                    .Where(arg => !string.IsNullOrWhiteSpace(arg.Value))
                    .Select(arg => arg.Value.Trim())
                    .ToList();
                mcpServer.EnvironmentVariables = FormModel.EnvironmentVariables
                    .Where(env => !string.IsNullOrWhiteSpace(env.Name))
                    .ToDictionary(
                        env => env.Name.Trim(),
                        env => string.IsNullOrWhiteSpace(env.Value) ? null : env.Value.Trim()
                    );
            }
            else
            {
                mcpServer.Url = FormModel.Url;
                mcpServer.HttpHeaders = FormModel.HttpHeaders
                    .Where(env => !string.IsNullOrWhiteSpace(env.Name))
                    .ToDictionary(
                        env => env.Name.Trim(),
                        env => string.IsNullOrWhiteSpace(env.Value) ? null : env.Value.Trim()
                    );
            }

            var closeResult = CloseResult.Errored;
            
            try
            {
                if (_mcpServer.Id == null)
                {
                    await _configurationService.CreateMcpServerAsync(mcpServer);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' created");

                    closeResult = CloseResult.Created;
                }
                else
                {
                    mcpServer.Id = _mcpServer.Id;
                    await _configurationService.UpdateMcpServerAsync(mcpServer);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' updated");
                    
                    closeResult = CloseResult.Updated;
                }
                
                WeakReferenceMessenger.Default.Send(new SettingsHaveChangedMessage()
                {
                    SettingsType = SettingsType.McpServer
                });
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
    private async Task ExecuteDeleteCommandAsync()
    {
        var closeResult = CloseResult.Errored;
        
        try
        {
            if (_mcpServer.Id != null)
            {
                var yesDelete = await _dialogService.ShowConfirmationDialogAsync(
                    "Delete MCP Server", "This will also remove all tools associated with this MCP Server. Continue?");

                if (yesDelete)
                {
                    await _configurationService.DeleteMcpServerAsync(_mcpServer.Id.Value);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' deleted");
                    
                    WeakReferenceMessenger.Default.Send(new SettingsHaveChangedMessage()
                    {
                        SettingsType = SettingsType.McpServer
                    });
                }
                
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
/// Represents a model for the MCP Server form data used to bind properties and validate inputs in the MCP Server view.
/// </summary>
public partial class McpServerFormDataModel : ObservableValidator
{
    /// <summary>
    /// Represents if the MCP server is new or existing
    /// </summary>
    [ObservableProperty] private bool? _isExisting;
    
    /// <summary>
    /// Represents the name of the MCP Server.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Name is required")] private string? _name;
    
    /// <summary>
    /// Represents the description of the MCP Server.
    /// </summary>
    [ObservableProperty] string? _description;
    
    /// <summary>
    /// Represents the location of the MCP Server.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Location is required")] private ServerLocation? _location;
    
    /// <summary>
    /// Represents the command for a Local MCP Server.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [ConditionalRequired(nameof(Location), ServerLocation.Local, ErrorMessage = "Command is required")] 
    private string? _command;
    
    /// <summary>
    /// Represents the collection of arguments for a Local Server
    /// </summary>
    [ObservableProperty] private ObservableCollection<ArgumentItem> _arguments = new();
    
    /// <summary>
    /// Represents the collection of environment variables for a Local Server
    /// </summary>
    [ObservableProperty] private ObservableCollection<EnvironmentVariableItem> _environmentVariables = new();
    
    /// <summary>
    /// Represents the URL of a Remote MCP Server.
    /// </summary>
    [ObservableProperty] 
    [NotifyDataErrorInfo] 
    [ConditionalRequired(nameof(Location), ServerLocation.Remote, ErrorMessage = "URL is required")] 
    private string? _url;
    
    /// <summary>
    /// Represents the collection of HTTP Headers for a Remote Server
    /// </summary>
    [ObservableProperty] private ObservableCollection<HttpHeaderItem> _httpHeaders = new();

    /// <summary>
    /// Tracks whether validation has been explicitly triggered (e.g., on save attempt)
    /// </summary>
    private bool _validateHiddenProperties = false;

    /// <summary>
    /// Validates all properties of the current object and checks if any validation errors exist.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the current object has no validation errors.
    /// Returns true if the object is valid, otherwise false.
    /// </returns>
    public bool Validate()
    {
        _validateHiddenProperties = true;

        ValidateAllProperties();
        
        _validateHiddenProperties = false;
        
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
    
    /// <summary>
    /// Handles location changes to trigger re-validation of dependent properties.
    /// </summary>
    /// <param name="value">The new location value.</param>
    partial void OnLocationChanged(ServerLocation? value)
    {
        // Only validate these hidden properties when the location just changed and form validation from user event
        // occurred
        if (_validateHiddenProperties)
        {
            ValidateProperty(Command, nameof(Command));
            ValidateProperty(Url, nameof(Url));
        }
    }
}

public partial class ArgumentItem : ObservableObject
{
    [ObservableProperty]
    private string _value = string.Empty;
}

public partial class EnvironmentVariableItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _value = string.Empty;
}

public partial class HttpHeaderItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _value = string.Empty;
}
