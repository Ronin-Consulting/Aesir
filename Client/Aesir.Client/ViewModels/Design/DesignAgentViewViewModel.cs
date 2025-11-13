using System;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Provides the design-time implementation of the AgentViewViewModel class. This class is
/// primarily used for providing mock data and behavior in a design-time environment.
/// </summary>
/// <remarks>
/// This view model is used to simulate the AgentViewViewModel at design time,
/// with preconfigured mock properties, commands, and data that are used to facilitate
/// UI development in design tools. The data and implementation are static and not connected
/// to real runtime services or data.
/// </remarks>
public class DesignAgentViewViewModel : AgentViewViewModel
{
    /// <summary>
    /// Represents a design-time implementation of the AgentViewViewModel class.
    /// </summary>
    /// <remarks>
    /// The DesignAgentViewViewModel class provides specific configurations for
    /// testing or design-time scenarios. It initializes an instance with hardcoded
    /// values representing an agent and other properties for use in design environments.
    /// </remarks>
    public DesignAgentViewViewModel() : base(new AesirAgentBase()
    {
        Id = Guid.NewGuid(),
        Name = "My Test Agent",
        Description = "",
        ChatPromptPersona = PromptPersona.Military,
        ChatCustomPromptContent = null,
        ChatInferenceEngineId = Guid.NewGuid(),
        ChatModel = "qwen3:32b-q4_K_M",
        ChatTemperature = 1.0,
        ChatTopP = 1.0,
        ChatMaxTokens = 10000
    }, new NoOpNotificationService(), new NoOpConfigurationService(), new NoOpModelService())
    {
        AvailableTools = new ObservableCollection<AesirToolBase>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "RAG"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Web"
            }
        };
        IsDirty = false;
        EditCustomPromptCommand = new RelayCommand(() => { });
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
    }
}