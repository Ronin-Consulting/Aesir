using System;
using System.Collections.Generic;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

/// <summary>
/// Provides the design-time implementation of the McpServerViewViewModel class. This class is
/// primarily used for providing mock data and behavior in a design-time environment.
/// </summary>
/// <remarks>
/// This view model is used to simulate the McpServerViewViewModel at design time,
/// with preconfigured mock properties, commands, and data that are used to facilitate
/// UI development in design MCP Servers. The data and implementation are static and not connected
/// to real runtime services or data.
/// </remarks>
public class DesignMcpServerViewViewModel : McpServerViewViewModel
{
    /// <summary>
    /// Represents a design-time implementation of the McpServerViewViewModel class.
    /// </summary>
    /// <remarks>
    /// The DesignMcpServerViewViewModel class provides specific configurations for
    /// testing or design-time scenarios. It initializes an instance with hardcoded
    /// values representing an MCP Server and other properties for use in design environments.
    /// </remarks>
    public DesignMcpServerViewViewModel() : base(new AesirMcpServerBase()
    {
        Id = Guid.NewGuid(),
        Name = "My Test MCP Server",
        Description = "This is a test of a very long description. There could be a lot more words here to show. Just depends on how many lines of text you'd like to see pop-up in the description. At minimum I'd like to see maybe 5 lines or so.",
        Location = ServerLocation.Local,
        Command = "/Users/blangford/Documents/server.sh",
        Arguments = [ "stdio" ],
        EnvironmentVariables = new Dictionary<string, string?>()
        {
            { "Name1", "Value1" }
        },
        Url = null,
        HttpHeaders = new Dictionary<string, string?>()
    }, new NoOpNotificationService(), new NoOpConfigurationService())
    {
        IsDirty = false;
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
        DeleteArgumentCommand = new RelayCommand(() => { });
        AddArgumentCommand = new RelayCommand(() => { });
        DeleteEnvironmentVariableCommand = new RelayCommand(() => { });
        AddEnvironmentVariableCommand = new RelayCommand(() => { });
        DeleteHttpHeaderCommand = new RelayCommand(() => { });
        AddHttpHeaderCommand = new RelayCommand(() => { });
    }
}