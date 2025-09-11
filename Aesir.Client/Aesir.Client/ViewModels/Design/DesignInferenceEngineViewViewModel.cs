using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Provides the design-time implementation of the InferenceEngineViewViewModel class. This class is
/// primarily used for providing mock data and behavior in a design-time environment.
/// </summary>
/// <remarks>
/// This view model is used to simulate the InferenceEngineViewViewModel at design time,
/// with preconfigured mock properties, commands, and data that are used to facilitate
/// UI development in design tools. The data and implementation are static and not connected
/// to real runtime services or data.
/// </remarks>
public class DesignInferenceEngineViewViewModel : InferenceEngineViewViewModel
{
    /// <summary>
    /// Represents a design-time implementation of the InferenceEngineViewViewModel class.
    /// </summary>
    /// <remarks>
    /// The DesignInferenceEngineViewViewModel class provides specific configurations for
    /// testing or design-time scenarios. It initializes an instance with hardcoded
    /// values representing an inference engine and other properties for use in design environments.
    /// </remarks>
    public DesignInferenceEngineViewViewModel() : base(new AesirInferenceEngineBase()
    {
        Id = Guid.NewGuid(),
        Name = "My Test Inference Engine",
        Description = "",
        Type = InferenceEngineType.Ollama,
        Configuration = new Dictionary<string, string?>()
    }, new NoOpNotificationService(), new NoOpConfigurationService())
    {
        IsDirty = false;
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
    }
}