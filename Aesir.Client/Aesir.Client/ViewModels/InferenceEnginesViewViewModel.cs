using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for managing inference engines in the application.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to inference engine interactions. It provides commands to display chat,
/// tools, and to add new inference engines. Additionally, it manages the collection of inference engines
/// and tracks the selected inference engine.
/// </remarks>
public class InferenceEnginesViewViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Represents a command that triggers the display of the chat interface.
    /// </summary>
    public ICommand ShowChat { get; protected set; }

    /// <summary>
    /// Represents a command that triggers the display of an interface for adding a new inference engine.
    /// </summary>
    public ICommand ShowAddInferenceEngine { get; protected set; }

    /// <summary>
    /// Represents a collection of inference engines displayed in the inference engines view.
    /// </summary>
    public ObservableCollection<AesirInferenceEngineBase> InferenceEngines { get; protected set; }

    /// <summary>
    /// Represents the currently selected inference engine from the collection of inference engines.
    /// This property is bound to the selection within the user interface and updates whenever
    /// a new inference engine is chosen. Triggers logic related to inference engine selection changes.
    /// </summary>
    public AesirInferenceEngineBase? SelectedInferenceEngine
    {
        get => _selectedInferenceEngine;
        set
        {
            if (SetProperty(ref _selectedInferenceEngine, value))
            {
                OnInferenceEngineSelected(value);
            }
        }
    }

    /// <summary>
    /// Represents the logger instance used for capturing and recording log messages
    /// within the context of the InferenceEnginesViewViewModel class. This includes logging
    /// errors, warnings, and informational messages related to the execution of
    /// various operations and application states in the view model.
    /// </summary>
    private readonly ILogger<InferenceEnginesViewViewModel> _logger;

    /// <summary>
    /// Provides navigation functionality to transition between various views or features
    /// within the application.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Provides access to configuration-related operations and data
    /// management for inference engines and tools within the system.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Backing field for the currently selected inference engine in the view model.
    /// </summary>
    private AesirInferenceEngineBase? _selectedInferenceEngine;

    /// Represents the view model for managing inference engines within the application.
    /// Provides commands to display the chat and inference engine creation interfaces.
    /// Maintains a collection of inference engines and tracks the currently selected inference engine.
    /// Integrates navigation and configuration services to coordinate application workflows.
    public InferenceEnginesViewViewModel(
        ILogger<InferenceEnginesViewViewModel> logger,
        INavigationService navigationService,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _configurationService = configurationService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowAddInferenceEngine = new RelayCommand(ExecuteShowAddInferenceEngine);

        InferenceEngines = new ObservableCollection<AesirInferenceEngineBase>();
    }

    /// Called when the view model is activated.
    /// Invokes an asynchronous operation to load inference engine data into the view model's collection.
    /// This method is designed to execute on the UI thread and ensures the proper initialization
    /// of inference engine-related data when the view model becomes active.
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadInferenceEnginesAsync);
    }

    /// <summary>
    /// Does the initial load of the inference engines.
    /// </summary>
    private async Task LoadInferenceEnginesAsync()
    {
        await RefreshInferenceEnginesAsync();
    }

    /// Asynchronously loads inference engines into the view model's InferenceEngines collection.
    /// Fetches the inference engines from the configuration service and populates the collection.
    /// Handles any exceptions that may occur during the loading process.
    public async Task RefreshInferenceEnginesAsync()
    {
        try
        {
            var inferenceEngines = await _configurationService.GetInferenceEnginesAsync();
            InferenceEngines.Clear();
            foreach (var inferenceEngine in inferenceEngines)
                InferenceEngines.Add(inferenceEngine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading inference engines: {ex.Message}");
        } 
    }

    /// Executes navigation to the Chat view within the application.
    /// Invokes the navigation service to display the Chat interface, facilitating interaction with chat-specific UI components.
    private void ExecuteShowChat()
    {
        _navigationService.NavigateToChat();
    }

    /// Executes the command to show the interface for adding a new inference engine.
    /// Sends a message indicating that the interface for inference engine details should be displayed.
    /// This method is bound to the `ShowAddInferenceEngine` command in the view model and is triggered
    /// when the corresponding user action is performed in the UI.
    private void ExecuteShowAddInferenceEngine()
    {
        WeakReferenceMessenger.Default.Send(new ShowInferenceEngineDetailMessage(null));
    }

    /// Handles logic when an inference engine is selected in the InferenceEnginesViewViewModel.
    /// Sends a message to display detailed information about the selected inference engine.
    /// <param name="selectedInferenceEngine">The inference engine that has been selected. If null, no action is taken.</param>
    private void OnInferenceEngineSelected(AesirInferenceEngineBase? selectedInferenceEngine)
    {
        if (selectedInferenceEngine != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowInferenceEngineDetailMessage(selectedInferenceEngine));
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

    /// Disposes of the resources used by the InferenceEnginesViewViewModel.
    /// Ensures proper release of managed resources and suppresses finalization
    /// to optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}