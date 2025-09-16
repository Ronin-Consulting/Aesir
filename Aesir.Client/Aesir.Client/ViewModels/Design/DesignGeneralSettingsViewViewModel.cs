using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Provides the design-time implementation of the GeneralSettingsViewViewModel class. This class is
/// primarily used for providing mock data and behavior in a design-time environment.
/// </summary>
/// <remarks>
/// This view model is used to simulate the GeneralSettingsViewViewModel at design time,
/// with preconfigured mock properties, commands, and data that are used to facilitate
/// UI development in design tools. The data and implementation are static and not connected
/// to real runtime services or data.
/// </remarks>
public class DesignGeneralSettingsViewViewModel : GeneralSettingsViewViewModel
{
    /// <summary>
    /// Represents a design-time implementation of the GeneralSettingsViewViewModel class.
    /// </summary>
    /// <remarks>
    /// The DesignGeneralSettingsViewViewModel class provides specific configurations for
    /// testing or design-time scenarios. It initializes an instance with hardcoded
    /// values representing an inference engine and other properties for use in design environments.
    /// </remarks>
    public DesignGeneralSettingsViewViewModel() : base(new NoOpNotificationService(), new NoOpConfigurationService(),
        new NoOpModelService())
    {
        IsDirty = false;
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
    }
}