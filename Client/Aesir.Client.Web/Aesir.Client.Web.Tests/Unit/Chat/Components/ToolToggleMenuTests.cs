using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ToolToggleMenuTests : TestContext
{
    public ToolToggleMenuTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Button State Tests

    [Fact]
    public void Button_IsDisabled_WhenNoTools()
    {
        // Arrange - No tools provided
        var tools = Array.Empty<AesirToolBase>();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Assert - Button should be disabled
        var button = cut.Find(".tool-toggle-menu-container button");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Button_IsDisabled_WhenOnlyRagToolPresent()
    {
        // Arrange - Only RAG tool (which is excluded from toggleable tools)
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = Guid.NewGuid(), ToolName = AesirTools.RagToolName, Name = "RAG" }
        };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Assert - Button should be disabled (RAG tool is not toggleable)
        var button = cut.Find(".tool-toggle-menu-container button");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Button_IsEnabled_WhenToggleableToolsAvailable()
    {
        // Arrange - Non-RAG tool with valid ID
        var tools = CreateToggleableTools();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Assert - Button should be enabled
        var button = cut.Find(".tool-toggle-menu-container button");
        button.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Button_IsDisabled_WhenIsDisabledParameter()
    {
        // Arrange
        var tools = CreateToggleableTools();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.IsDisabled, true));

        // Assert - Button should be disabled
        var button = cut.Find(".tool-toggle-menu-container button");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    #endregion

    #region Indicator Dot Tests

    [Fact]
    public void Button_HasIndicatorDot_WhenToolsEnabled()
    {
        // Arrange - Tools available and enabled (not in DisabledToolIds)
        var tools = CreateToggleableTools();
        var disabledToolIds = new HashSet<Guid>(); // Empty = all enabled

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, disabledToolIds));

        // Assert - Should have the "has-tools" class for indicator dot
        var wrapper = cut.Find(".tool-button-wrapper");
        wrapper.ClassList.Should().Contain("has-tools");
    }

    [Fact]
    public void Button_NoIndicatorDot_WhenAllToolsDisabled()
    {
        // Arrange - All tools disabled
        var toolId = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = toolId, ToolName = "WebTool", Name = "Web Search" }
        };
        var disabledToolIds = new HashSet<Guid> { toolId };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, disabledToolIds));

        // Assert - Should NOT have the "has-tools" class
        var wrapper = cut.Find(".tool-button-wrapper");
        wrapper.ClassList.Should().NotContain("has-tools");
    }

    [Fact]
    public void Button_NoIndicatorDot_WhenNoTools()
    {
        // Arrange
        var tools = Array.Empty<AesirToolBase>();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Assert - Should NOT have the "has-tools" class
        var wrapper = cut.Find(".tool-button-wrapper");
        wrapper.ClassList.Should().NotContain("has-tools");
    }

    #endregion

    #region Button Color Tests

    [Fact]
    public void Button_HasPrimaryColor_WhenToolsEnabled()
    {
        // Arrange
        var tools = CreateToggleableTools();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, new HashSet<Guid>()));

        // Assert - Button should have primary color class (MudBlazor uses mud-primary-text for icon buttons)
        var button = cut.Find(".tool-toggle-menu-container button");
        button.ClassList.Should().Contain("mud-primary-text");
    }

    [Fact]
    public void Button_HasDefaultColor_WhenAllToolsDisabled()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = toolId, ToolName = "WebTool", Name = "Web" }
        };
        var disabledToolIds = new HashSet<Guid> { toolId };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, disabledToolIds));

        // Assert - Button should NOT have primary color class (MudBlazor uses absence of color class for default)
        var button = cut.Find(".tool-toggle-menu-container button");
        button.ClassList.Should().NotContain("mud-primary-text");
    }

    #endregion

    #region Menu Tests

    [Fact]
    public async Task Menu_OpensOnButtonClick()
    {
        // Arrange
        var tools = CreateToggleableTools();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Act - Click button
        var button = cut.Find(".tool-toggle-menu-container button");
        await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Menu should be visible (backdrop visible indicates menu is open)
        cut.Markup.Should().Contain("tool-toggle-backdrop");
    }

    [Fact]
    public async Task Menu_ClosesOnBackdropClick()
    {
        // Arrange
        var tools = CreateToggleableTools();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Open menu first
        var button = cut.Find(".tool-toggle-menu-container button");
        await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Verify menu is open - backdrop element should exist
        var backdropElements = cut.FindAll(".tool-toggle-backdrop");
        backdropElements.Should().NotBeEmpty("backdrop should be visible when menu is open");

        // Act - Click backdrop to close
        var backdrop = cut.Find(".tool-toggle-backdrop");
        await backdrop.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Re-render and check state
        cut.Render();

        // After clicking backdrop, the backdrop element should not exist
        var closedBackdropElements = cut.FindAll(".tool-toggle-backdrop");
        closedBackdropElements.Should().BeEmpty("backdrop should be removed when menu is closed");
    }

    [Fact]
    public void Menu_HasCorrectToolCount_InTooltip()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = Guid.NewGuid(), ToolName = "WebTool", Name = "Web Search" },
            new AesirToolBase { Id = Guid.NewGuid(), ToolName = "CustomTool", Name = "Custom Tool" }
        };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, new HashSet<Guid>()));

        // Assert - Tooltip should show 2 enabled tools
        var button = cut.Find(".tool-toggle-menu-container button");
        button.GetAttribute("Title").Should().Contain("2 enabled");
    }

    #endregion

    #region RAG Tool Exclusion Tests

    [Fact]
    public void RagToolIsExcluded_FromToggleableTools()
    {
        // Arrange - Mix of RAG and non-RAG tools
        var ragToolId = Guid.NewGuid();
        var webToolId = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = ragToolId, ToolName = AesirTools.RagToolName, Name = "RAG" },
            new AesirToolBase { Id = webToolId, ToolName = "WebTool", Name = "Web Search" }
        };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, new HashSet<Guid>()));

        // Assert - Only 1 tool should be toggleable (RAG excluded)
        // The tooltip shows the count of enabled toggleable tools
        var button = cut.Find(".tool-toggle-menu-container button");
        button.GetAttribute("Title").Should().Contain("1 enabled");
    }

    [Fact]
    public void OnlyRagTool_DisablesButton()
    {
        // Arrange - Only RAG tool
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = Guid.NewGuid(), ToolName = AesirTools.RagToolName, Name = "RAG" }
        };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Assert - Button should be disabled (no toggleable tools)
        var button = cut.Find(".tool-toggle-menu-container button");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    #endregion

    #region Tooltip Tests

    [Fact]
    public void Button_HasCorrectTooltip_WhenToolsEnabled()
    {
        // Arrange
        var tools = CreateToggleableTools();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, new HashSet<Guid>()));

        // Assert - Title attribute should show enabled count
        var button = cut.Find(".tool-toggle-menu-container button");
        button.GetAttribute("Title").Should().Contain("1 enabled");
    }

    [Fact]
    public void Button_HasCorrectTooltip_WhenAllToolsDisabled()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = toolId, ToolName = "WebTool", Name = "Web" }
        };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, new HashSet<Guid> { toolId }));

        // Assert
        var button = cut.Find(".tool-toggle-menu-container button");
        button.GetAttribute("Title").Should().Contain("all disabled");
    }

    [Fact]
    public void Button_HasCorrectTooltip_WhenNoToggleableTools()
    {
        // Arrange
        var tools = Array.Empty<AesirToolBase>();

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools));

        // Assert
        var button = cut.Find(".tool-toggle-menu-container button");
        button.GetAttribute("Title").Should().Contain("No tools available");
    }

    #endregion

    #region Partial Disable Tests

    [Fact]
    public void Button_ShowsCorrectCount_WhenSomeToolsDisabled()
    {
        // Arrange - 3 tools, 1 disabled
        var tool1Id = Guid.NewGuid();
        var tool2Id = Guid.NewGuid();
        var tool3Id = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new AesirToolBase { Id = tool1Id, ToolName = "Tool1", Name = "Tool 1" },
            new AesirToolBase { Id = tool2Id, ToolName = "Tool2", Name = "Tool 2" },
            new AesirToolBase { Id = tool3Id, ToolName = "Tool3", Name = "Tool 3" }
        };
        var disabledToolIds = new HashSet<Guid> { tool2Id };

        // Render provider first for MudPopover support
        RenderComponent<MudPopoverProvider>();

        // Act
        var cut = RenderComponent<ToolToggleMenu>(parameters => parameters
            .Add(p => p.Tools, tools)
            .Add(p => p.DisabledToolIds, disabledToolIds));

        // Assert - Should show 2 enabled (3 total - 1 disabled)
        var button = cut.Find(".tool-toggle-menu-container button");
        button.GetAttribute("Title").Should().Contain("2 enabled");

        // Should still show indicator dot since some tools are enabled
        var wrapper = cut.Find(".tool-button-wrapper");
        wrapper.ClassList.Should().Contain("has-tools");
    }

    #endregion

    #region Helper Methods

    private static List<AesirToolBase> CreateToggleableTools()
    {
        return new List<AesirToolBase>
        {
            new AesirToolBase { Id = Guid.NewGuid(), ToolName = "WebTool", Name = "Web Search" }
        };
    }

    #endregion
}
