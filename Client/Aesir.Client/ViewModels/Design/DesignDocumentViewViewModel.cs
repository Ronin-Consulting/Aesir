using System;
using Aesir.Client.Models;
using Aesir.Client.Services.Implementations.NoOp;
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
public class DesignDocumentViewViewModel : DocumentViewViewModel
{
    /// <summary>
    /// Represents a design-time implementation of the DocumentViewViewModel class.
    /// </summary>
    /// <remarks>
    /// The DesignDocumentViewViewModel class provides specific configurations for
    /// testing or design-time scenarios. It initializes an instance with hardcoded
    /// values representing an document and other properties for use in design environments.
    /// </remarks>
    public DesignDocumentViewViewModel() : base(new AesirDocument()
    {
        Id = Guid.NewGuid(),
        FileName = "My Filename",
        CreatedAt = DateTime.Now,
        FileSize = 9999,
        MimeType = "application/octet-stream",
        UpdatedAt = DateTime.Now
    }, new NoOpNotificationService(), new NoOpDocumentCollectionService(), new NoOpChatHistoryService())
    {
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
    }
}