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
/// Represents the design-time implementation of the <c>McpServersViewViewModel</c>.
/// Provides mock data and commands to aid in the design and development of the Agents View interface.
/// </summary>
public class DesignMcpServersViewViewModel : McpServersViewViewModel
{
    /// <summary>
    /// A design-time implementation of the <see cref="McpServersViewViewModel"/> class,
    /// primarily used for populating sample data and providing commands suitable for
    /// use during UI design in MCP Servers like visual editors.
    /// </summary>
    public DesignMcpServersViewViewModel() : base(NullLogger<McpServersViewViewModel>.Instance, new NoOpNavigationService(), new NoOpConfigurationService())
    {
        ShowChat = new RelayCommand(() => { });
        ShowAddMcpServer = new RelayCommand(() => { });
        
        var mcpServers = new List<AesirMcpServerBase> 
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Email",
                Description = "Supports sending email through the tool's configured account",
                Command = "/Users/Byron/Documents/CatPictures/cat-pic-downloader.sh"
            }
        };
        McpServers = new ObservableCollection<AesirMcpServerBase>(mcpServers);
    }
}