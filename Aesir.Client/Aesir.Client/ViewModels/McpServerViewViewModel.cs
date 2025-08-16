using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    private AesirMcpServerBase _mcpServer;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including MCP Servers
    /// and MCP Servers, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;

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
            IConfigurationService configurationService)
    {
        _mcpServer = mcpServer;
        _notificationService = notificationService;
        _configurationService = configurationService;
        
        FormModel = new()
        {
            IsExisting = mcpServer.Id.HasValue,
            Name = mcpServer.Name,
            Description = mcpServer.Description
        };
        IsDirty = false;
        SaveCommand = new AsyncRelayCommand(ExecuteSaveCommandAsync);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommandAsync);
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
    /// TODO
    /// </summary>
    /// <returns>A task representing the asynchronous operation of loading data.</returns>
    private async Task LoadAvailableAsync()
    {
        await Task.CompletedTask;
        
        // TODO actually load - list mcp server tools .. could be a lot of MCP Servers, maybe wait till selected???
        //AvailableMcpServerTools.Clear();
        //AvailableMcpServerTools.Add("gpt-4.1-2025-04-14");
        //AvailableMcpServerTools.Add("qwen3:32b-q4_K_M");
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
                Command = "/Users/ryan/Documents/Development/python/mcp-email-server/mcp-email/bin/mcp-email-server",
                Arguments = ["stdio"],
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "MyEnvVar", "value"}
                }
            };
            
// TODO need tool arguments on the tool when MCP Server

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
                await _configurationService.DeleteMcpServerAsync(_mcpServer.Id.Value);

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
/// Represents a model for the MCP Server form data used to bind properties and validate inputs in the MCP Server view.
/// </summary>
public partial class McpServerFormDataModel : ObservableValidator
{
    /// <summary>
    /// Represents if the MCP server is new or existing
    /// </summary>
    [ObservableProperty] private bool? _isExisting;
    
    /// <summary>
    /// Represents the name of the MCP Server, required for validation and user input.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Name is required")] private string? _name;
    
    /// <summary>
    /// Represents the description of the MCP Server, required for validation and user input.
    /// </summary>
    [ObservableProperty] string? _description;

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