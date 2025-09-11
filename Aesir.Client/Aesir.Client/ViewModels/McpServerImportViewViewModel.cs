using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Ursa.Controls;

namespace Aesir.Client.ViewModels;

public partial class McpServerImportViewViewModel : ObservableValidator, IDialogContext
{
    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including MCP Servers
    /// and MCP Servers, within the application.
    /// </summary>
    private readonly IConfigurationService _configurationService;
    
    [ObservableProperty]
    private string _clientConfigJson = string.Empty;
    
    [ObservableProperty]
    private string? _validationError;
    
    public ICommand OkCommand { get; set; }
    
    public ICommand CancelCommand { get; set; }
    
    public event EventHandler<object?>? RequestClose;
    
    public AesirMcpServerBase? GeneratedMcpServer { get; set; }
    
    public McpServerImportViewViewModel( 
        INotificationService notificationService,
        IConfigurationService configurationService)
    {
        _notificationService = notificationService;
        _configurationService = configurationService;
        
        OkCommand = new AsyncRelayCommand(ExecuteOkCommandAsync);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
    }

    private async Task ExecuteOkCommandAsync()
    {
        // Clear previous validation error
        ValidationError = null;
        
        // Manual validation only when submitting
        if (string.IsNullOrWhiteSpace(ClientConfigJson))
        {
            ValidationError = "Client configuration JSON is required";
            return;
        }

        try
        {
            GeneratedMcpServer = await _configurationService.CreateMcpServerFromConfigAsync(ClientConfigJson);
            Close(DialogResult.OK);
        }
        catch (Exception e)
        {
            // Set the validation error that can be displayed on the form
            ValidationError = "Unabled to parse the Client configuration JSON";
        }
    }

    private void ExecuteCancelCommand()
    {
        Close(DialogResult.Cancel);
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
        Close(DialogResult.Cancel);
    }

    /// <summary>
    /// Triggers the request to close the associated view or dialog by invoking the <see cref="RequestClose"/> event.
    /// </summary>
    /// <param name="dialogResult">The results to send to the close event</param>
    private void Close(DialogResult dialogResult)
    {
        RequestClose?.Invoke(this, dialogResult);
    }
}