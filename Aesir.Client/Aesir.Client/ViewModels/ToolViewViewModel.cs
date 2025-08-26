using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for tool-related views, providing properties, commands,
/// and events to manage tool configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> and implements <see cref="IDialogContext"/>,
/// enabling it to work with MVVM patterns and dialog-based user interactions. It manages
/// collections of available models, tools, and prompts, as well as command bindings for
/// managing tool configuration lifecycle actions such as save, cancel, and delete.
/// </remarks>
/// <example>
/// Typically used in the context of tool detail views within the application. The view model
/// is activated and bound to the associated user control for displaying and managing tool
/// data.
/// </example>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IDialogContext" />
public partial class ToolViewViewModel : ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Represents the underlying tool configuration and details used by the view model, including properties such as ID, name, and type.
    /// </summary>
    private AesirToolBase _tool;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including tools
    /// and tools, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;
    
    /// <summary>
    /// Represents the initially selected McpServerId
    /// </summary>
    private Guid? _initialMcpServerId;

    /// <summary>
    /// Represents the form data model for the tool view, used to handle data
    /// binding and validation within the ToolViewViewModel.
    /// </summary>
    [ObservableProperty] private ToolFormDataModel _formModel;

    /// <summary>
    /// Collection of available tool types that can be selected or used within the application.
    /// </summary>
    public ObservableCollection<ToolType> AvailableTypes { get; } = new(Enum.GetValues<ToolType>());

    /// <summary>
    /// Collection of available MCP Servers that can be selected or used within the application.
    /// </summary>
    public ObservableCollection<AesirMcpServerBase> AvailableMcpServers { get; set; }

    /// <summary>
    /// Collection of available MCP Server Tools that can be selected or used within the application.
    /// </summary>
    public ObservableCollection<string> AvailableMcpServerTools { get; set; }

    /// <summary>
    /// Indicates whether the view model has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Command executed to save the current state or data of the tool view.
    /// </summary>
    public ICommand SaveCommand { get; set; }

    /// <summary>
    /// Command used to cancel the current operation or revert changes in the Tool View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Command used to delete the associated tool or entity.
    /// </summary>
    public ICommand DeleteCommand { get; set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;

    /// Represents the view model for the tool view. Handles the binding of tool data
    /// and communication between the user interface and underlying services.
    public ToolViewViewModel(AesirToolBase tool, 
            INotificationService notificationService,
            IConfigurationService configurationService)
    {
        _tool = tool;
        _notificationService = notificationService;
        _configurationService = configurationService;

        _initialMcpServerId = tool.McpServerId;
        
        FormModel = new()
        {
            IsExisting = tool.Id.HasValue,
            Name = tool.Name,
            Type = tool.Type,
            Description = tool.Description,
            McpServerTool = tool.McpServerTool
        };
        IsDirty = false;
        SaveCommand = new AsyncRelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommand);
        
        AvailableMcpServers = new ObservableCollection<AesirMcpServerBase>();
        AvailableMcpServerTools = new ObservableCollection<string>();
    
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
    /// Asynchronously loads and initializes available tools, model sources, and models for the tool.
    /// This method retrieves available tools through the configuration service, updates relevant observable collections
    /// with the data, and configures the tools associated with the current tool.
    /// Clears and repopulates available collections for chat models, embedding models, and vision models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of loading data.</returns>
    private async Task LoadAvailableAsync()
    {       
        // get available MCP Servers
        var svailableMcpServers = await _configurationService.GetMcpServersAsync();
        AvailableMcpServers.Clear();
        foreach (var svailableMcpServer in svailableMcpServers)
            AvailableMcpServers.Add(svailableMcpServer);

        // establish the selected MCP Server
        if (_initialMcpServerId != null)
        {
            FormModel.McpServer = AvailableMcpServers.First(s => s.Id == _initialMcpServerId);
        }

        // get available MCP Server Tools if available
        await LoadMcpServerTools();
    }

    /// <summary>
    /// Loads MCP server tools
    /// </summary>
    private async Task LoadMcpServerTools()
    {
        AvailableMcpServerTools.Clear();
        
       if (FormModel.McpServer == null)
            return;

       try
       {
           var availableMcpServerTools = await _configurationService.GetMcpServerTools(FormModel.McpServer.Id.Value);
           foreach (var mcpServerTool in availableMcpServerTools)
               AvailableMcpServerTools.Add(mcpServerTool.Name);
       }
       catch (Exception e)
       {
           // TODO
       }
        
       // The SelectedItem wasn't in the ComboBox's collection initially since we just loaded it. Ideally it
       // would re-evaluate, but it doesn't want to. There is a very talked about issue with this timing.
       // We have to force the ComboBox to re-bind the SelectedItem to get it to work.
        
       var currentMcpServerTool = FormModel.McpServerTool;
        
       FormModel.McpServerTool = null;

       // Be a little wiser and only set the value if they exist in the collections (may have been removed from db)
       if (!string.IsNullOrEmpty(currentMcpServerTool) && AvailableMcpServerTools.Contains(currentMcpServerTool))
           FormModel.McpServerTool = currentMcpServerTool;
    }

    /// <summary>
    /// Handles property changes in the form model to trigger related updates.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data containing the name of the property that changed.</param>
    private void OnFormModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToolFormDataModel.McpServer))
        {
            _ = LoadMcpServerTools();
        }
    }


    /// <summary>
    /// Executes the logic to save the changes made in the form.
    /// This method validates the form model before saving the data.
    /// If validation is successful, the method applies the changes from the form model to the underlying tool object,
    /// persists the data, and displays a success notification to the user.
    /// The method also initiates the closure of the associated dialog or view.
    /// </summary>
    private async Task ExecuteSaveCommand()
    {
        if (FormModel.Validate())
        {
            var tool = new AesirToolBase()
            {
                Name = FormModel.Name,
                Description = FormModel.Description,
                Type = FormModel.Type,
                McpServerId = FormModel.McpServer?.Id,
                McpServerTool = FormModel.McpServerTool
            };

            var closeResult = CloseResult.Errored;
            
            try
            {
                if (_tool.Id == null)
                {
                    await _configurationService.CreateToolAsync(tool);

                    _notificationService.ShowSuccessNotification("Success", $"'{FormModel.Name}' created");

                    closeResult = CloseResult.Created;
                }
                else
                {
                    tool.Id = _tool.Id;
                    await _configurationService.UpdateToolAsync(tool);

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
            if (_tool.Id != null)
            {
                await _configurationService.DeleteToolAsync(_tool.Id.Value);

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
    
    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    
        FormModel.PropertyChanged -= OnFormModelPropertyChanged;
    }
}

/// <summary>
/// Represents a model for the tool form data used to bind properties and validate inputs in the tool view.
/// </summary>
public partial class ToolFormDataModel : ObservableValidator
{
    /// <summary>
    /// Represents if the agent is new or existing
    /// </summary>
    [ObservableProperty] private bool? _isExisting;
    
    /// <summary>
    /// Represents the name of the tool, required for validation and user input.
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Name is required")] private string? _name;
    
    /// <summary>
    /// Represents the description of the tool, required for validation and user input.
    /// </summary>
    [ObservableProperty] string? _description;
    
    /// <summary>
    /// Represents the type of the tool
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required (ErrorMessage = "Type is required")] private ToolType? _type;
    
    /// <summary>
    /// Represents the specific MCP server selected
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [ConditionalRequired(nameof(Type), ToolType.McpServer, ErrorMessage = "Server is required")] private AesirMcpServerBase? _mcpServer;
    
    /// <summary>
    /// Represents the specific MCP server tool selected
    /// </summary>
    [ObservableProperty] [NotifyDataErrorInfo] [ConditionalRequired(nameof(Type), ToolType.McpServer, ErrorMessage = "Server Tool is required")] private string? _mcpServerTool;

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