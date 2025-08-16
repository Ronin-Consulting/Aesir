using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Represents the design-time implementation of the <c>ToolsViewViewModel</c>.
/// Provides mock data and commands to aid in the design and development of the Agents View interface.
/// </summary>
public class DesignToolsViewViewModel : ToolsViewViewModel
{
    /// <summary>
    /// A design-time implementation of the <see cref="ToolsViewViewModel"/> class,
    /// primarily used for populating sample data and providing commands suitable for
    /// use during UI design in tools like visual editors.
    /// </summary>
    public DesignToolsViewViewModel() : base(NullLogger<ToolsViewViewModel>.Instance, new NoOpNavigationService(), new NoOpConfigurationService())
    {
        ShowChat = new RelayCommand(() => { });
        ShowAddTool = new RelayCommand(() => { });
        
        var tools = new List<AesirToolBase> 
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Web",
                Type = ToolType.Internal,
                Description = "Provides the ability to search the web using google"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "RAG",
                Type = ToolType.Internal,
                Description = "Supports searching documents with Retrieval Augmented Generation"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Email",
                Type = ToolType.McpServer,
                Description = "Supports sending email through the tool's configured account"
            }
        };
        Tools = new ObservableCollection<AesirToolBase>(tools);
    }
}