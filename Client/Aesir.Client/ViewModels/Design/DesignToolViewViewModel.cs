using System;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Provides the design-time implementation of the ToolViewViewModel class. This class is
/// primarily used for providing mock data and behavior in a design-time environment.
/// </summary>
/// <remarks>
/// This view model is used to simulate the ToolViewViewModel at design time,
/// with preconfigured mock properties, commands, and data that are used to facilitate
/// UI development in design tools. The data and implementation are static and not connected
/// to real runtime services or data.
/// </remarks>
public class DesignToolViewViewModel : ToolViewViewModel
{
    /// <summary>
    /// Represents a design-time implementation of the ToolViewViewModel class.
    /// </summary>
    /// <remarks>
    /// The DesignToolViewViewModel class provides specific configurations for
    /// testing or design-time scenarios. It initializes an instance with hardcoded
    /// values representing an tool and other properties for use in design environments.
    /// </remarks>
    public DesignToolViewViewModel() : base(new AesirToolBase()
    {
        Id = Guid.NewGuid(),
        Name = "My Test Tool",
        Description = "This is a test of a very long description. There could be a lot more words here to show. Just depends on how many lines of text you'd like to see pop-up in the description. At minimum I'd like to see maybe 5 lines or so.",
        Type = ToolType.Internal,
        IconName = "World"
    }, new NoOpNotificationService(), new NoOpDialogService(), new NoOpConfigurationService())
    {
        IsDirty = false;
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
    }
}