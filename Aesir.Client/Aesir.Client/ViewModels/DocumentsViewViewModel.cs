using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.Views;
using Aesir.Common.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for managing documents in the application.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to tool interactions. It provides commands to display chat,
/// tools, and to add new tools. Additionally, it manages the collection of tools
/// and tracks the selected tool.
/// </remarks>
public class DocumentsViewViewModel : ObservableRecipient, IDisposable, IRecipient<FileDownloadMessage>
{
    /// <summary>
    /// Represents a command that triggers the display of the chat interface.
    /// </summary>
    public ICommand ShowChat { get; protected set; }

    /// <summary>
    /// Represents a command that triggers the display of an interface for document details.
    /// </summary>
    public ICommand ShowDocumentDetails { get; protected set; }

    /// <summary>
    /// Represents a command that detects a click on the grid as a reselect.
    /// </summary>
    public ICommand ReselectFromGrid { get; protected set; }

    /// <summary>
    /// Represents a collection of tools displayed in the tools view.
    /// </summary>
    public ObservableCollection<AesirDocument> Documents { get; protected set; }

    /// <summary>
    /// Represents the currently selected tool from the collection of tools.
    /// This property is bound to the selection within the user interface and updates whenever
    /// a new tool is chosen. Triggers logic related to tool selection changes.
    /// </summary>
    public AesirDocument? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (SetProperty(ref _selectedDocument, value))
            {
                OnDocumentSelected(value);
            }
        }
    }

    /// <summary>
    /// Represents the logger instance used for capturing and recording log messages
    /// within the context of the ToolsViewViewModel class. This includes logging
    /// errors, warnings, and informational messages related to the execution of
    /// various operations and application states in the view model.
    /// </summary>
    private readonly ILogger<ToolsViewViewModel> _logger;

    /// <summary>
    /// Provides navigation functionality to transition between various views or features
    /// within the application.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Provides access to configuration-related operations and data
    /// management for agents and tools within the system.
    /// </summary>
    private readonly IDocumentCollectionService _documentCollectionService;

    /// <summary>
    /// Backing field for the currently selected document in the view model.
    /// </summary>
    private AesirDocument? _selectedDocument;

    /// Represents the view model for managing documents within the application.
    /// Provides commands to display the chat and tool creation interfaces.
    /// Integrates navigation and configuration services to coordinate application workflows.
    public DocumentsViewViewModel(
        ILogger<ToolsViewViewModel> logger,
        INavigationService navigationService,
        IDocumentCollectionService documentCollectionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _documentCollectionService = documentCollectionService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowDocumentDetails = new RelayCommand(ExecuteShowDocumentDetails);
        ReselectFromGrid = new RelayCommand(ExecuteReselectFromGrid);

        Documents = new ObservableCollection<AesirDocument>();
    }

    /// Called when the view model is activated.
    /// Invokes an asynchronous operation to load tool data into the view model's collection.
    /// This method is designed to execute on the UI thread and ensures the proper initialization
    /// of tool-related data when the view model becomes active.
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadDocumentsAsync);
    }

    /// <summary>
    /// Does the initial load of the tools.
    /// </summary>
    private async Task LoadDocumentsAsync()
    {
        await RefreshDocumentsAsync();
    }

    /// Asynchronously loads agents into the view model's Agents collection.
    /// Fetches the agents from the configuration service and populates the collection.
    /// Handles any exceptions that may occur during the loading process.
    public async Task RefreshDocumentsAsync()
    {
        try
        {
            var docs = await _documentCollectionService.GetDocumentsAsync();
            Documents.Clear();
            foreach (var doc in docs)
                Documents.Add(doc);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tools: {ex.Message}");
        }
    }

    /// Executes navigation to the Chat view within the application.
    /// Invokes the navigation service to display the Chat interface, facilitating interaction with chat-specific UI components.
    private void ExecuteShowChat()
    {
        _navigationService.NavigateToChat();
    }

    /// Executes the command to show the interface for Document details.
    /// Sends a message indicating that the interface for Document details should be displayed.
    private void ExecuteShowDocumentDetails()
    {
        WeakReferenceMessenger.Default.Send(new ShowDocumentDetailMessage(SelectedDocument));   
    }

    /// Executes the command to show the interface for re-selecting a document when the grid is clicked.
    private void ExecuteReselectFromGrid()
    {
        WeakReferenceMessenger.Default.Send(new ShowDocumentDetailMessage(SelectedDocument));   
    }

    /// Handles logic when a Document is selected in the ToolsViewViewModel.
    /// Sends a message to display detailed information about the selected tool.
    /// <param name="selectedDocument">The Document that has been selected. If null, no action is taken.</param>
    private void OnDocumentSelected(AesirDocument? selectedDocument)
    {
        if (selectedDocument != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowDocumentDetailMessage(selectedDocument));
        }
    }

    /// Releases the resources used by the view model.
    /// Cleans up unmanaged resources and other disposable objects when the object is no longer needed.
    /// <param name="disposing">Indicates whether to release managed resources along with unmanaged resources.
    /// If set to true, both managed and unmanaged resources are disposed; if false, only unmanaged resources are released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// Disposes of the resources used by the ToolsViewViewModel.
    /// Ensures proper release of managed resources and suppresses finalization
    /// to optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Receive(FileDownloadMessage message)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            await SaveFilePickerAsync(message.FileName);
        });
    }
    
    private async Task SaveFilePickerAsync(string filePath)
    {
        // Get the top-level window or control to access the StorageProvider
        var topLevel = TopLevel.GetTopLevel(GetTopLevelControl());
        var fileName = Path.GetFileName(filePath);  
        
        if (topLevel?.StorageProvider == null)
        {
            _logger.LogWarning("Storage provider not available");
        }

        if (topLevel != null)
        {
            // Open a Save File dialog
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Download File",
                SuggestedFileName = Path.GetFileName(filePath), 
                ShowOverwritePrompt = true
            });

            if (file != null)
            {
                string localFilePath = file.Path.LocalPath;
                await DownloadFileAsync(fileName, localFilePath);
            }
        }
    } 
    
    /// Downloads a file from a remote source and saves it to a specified local file path.
    /// <param name="fileName">The name of the file to download from the remote source.</param>
    /// <param name="localFilePath">The local file path where the downloaded file will be saved.</param>
    /// <returns>A task that represents the asynchronous file download operation. It completes when the file has been fully downloaded and saved.</returns>
    private async Task DownloadFileAsync(string fileName, string localFilePath)
    {
        try
        {
            await using var contentStream = await _documentCollectionService.GetFileContentStreamAsync(fileName);
            // Create a FileStream to write the downloaded content to a local file
            await using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            // Copy the content from the network stream to the file stream
            await contentStream.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            // Handle other potential errors during file writing
            _logger.LogError(ex, "Error saving file to {LocalPath}", localFilePath);
            throw;
        }
    }
    
    
    /// Retrieves the top-level control of the application based on the application's lifetime configuration.
    /// Determines whether the application is running with a classic desktop style or in a single-view environment
    /// and returns the respective top-level control if available.
    /// Logs an error and returns null in case of any exceptions during retrieval.
    /// <returns>
    /// The ContentControl representing the top-level application control, or null if it cannot be determined.
    /// </returns>
    private ContentControl? GetTopLevelControl()
    {
        try
        {
            return Application.Current?.ApplicationLifetime switch
            {
                IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
                ISingleViewApplicationLifetime singleView => singleView.MainView as ContentControl,
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top level control");
            return null;
        }
    }

}