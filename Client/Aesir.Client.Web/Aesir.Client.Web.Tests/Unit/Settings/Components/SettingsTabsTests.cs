using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Components;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class SettingsTabsTests : TestContext
{
    private readonly Mock<ISettingsTabRegistry> _mockTabRegistry;

    public SettingsTabsTests()
    {
        // Create the Observability tab definition (normally registered by the Observability module)
        var observabilityTab = new SettingsTabDefinition
        {
            TabId = "observability",
            Label = "Observability",
            Icon = "Timeline",
            Priority = 100,
            ContentComponentType = typeof(MudText) // Placeholder for testing
        };

        _mockTabRegistry = new Mock<ISettingsTabRegistry>();
        _mockTabRegistry.Setup(x => x.GetTabs()).Returns(new List<SettingsTabDefinition> { observabilityTab });
        _mockTabRegistry.Setup(x => x.GetTab("observability")).Returns(observabilityTab);
        _mockTabRegistry.Setup(x => x.GetTab(It.Is<string>(s => s != "observability"))).Returns((SettingsTabDefinition?)null);

        Services.AddSingleton(_mockTabRegistry.Object);
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void RendersConfigurationGroup()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        cut.Markup.Should().Contain("Configuration");
    }

    [Fact]
    public void RendersUtilitiesGroup()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        cut.Markup.Should().Contain("Utilities");
    }

    [Theory]
    [InlineData("General")]
    [InlineData("Inference Engines")]
    [InlineData("MCP Servers")]
    [InlineData("Tools")]
    [InlineData("Agents")]
    [InlineData("Observability")]
    public void RendersAllTabLabels(string tabLabel)
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        cut.Markup.Should().Contain(tabLabel);
    }

    [Fact]
    public void RendersSetupWizardButton()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        cut.Markup.Should().Contain("Run Setup Wizard");
    }

    [Fact]
    public void SetupWizardButton_HasCorrectHref()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        var wizardButton = cut.FindAll("a").FirstOrDefault(a => a.TextContent.Contains("Run Setup Wizard"));
        wizardButton.Should().NotBeNull();
        wizardButton!.GetAttribute("href").Should().Be("/setup");
    }

    [Fact]
    public void SelectsCorrectTab_WhenSelectedTabIsGeneral()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        var selectedTabs = cut.FindAll(".settings-tab-item.selected");
        selectedTabs.Should().HaveCount(1);
        selectedTabs[0].TextContent.Should().Contain("General");
    }

    [Theory]
    [InlineData("general", "General")]
    [InlineData("inference-engines", "Inference Engines")]
    [InlineData("mcp-servers", "MCP Servers")]
    [InlineData("tools", "Tools")]
    [InlineData("agents", "Agents")]
    [InlineData("observability", "Observability")]
    public void SelectsCorrectTab_ForEachTabId(string tabId, string expectedLabel)
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, tabId));

        // Assert
        var selectedTab = cut.Find(".settings-tab-item.selected");
        selectedTab.TextContent.Should().Contain(expectedLabel);
    }

    [Fact]
    public async Task InvokesOnTabChanged_WhenTabClicked()
    {
        // Arrange
        string? changedTabId = null;
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general")
            .Add(p => p.OnTabChanged, EventCallback.Factory.Create<string>(this, (tabId) => changedTabId = tabId)));

        // Act - Find and click the Agents tab
        var agentsTab = cut.FindAll(".settings-tab-item").First(t => t.TextContent.Contains("Agents"));
        await agentsTab.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        changedTabId.Should().Be("agents");
    }

    [Fact]
    public void RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general")
            .AddChildContent("<div class='test-content'>Test Content</div>"));

        // Assert
        cut.Markup.Should().Contain("test-content");
        cut.Markup.Should().Contain("Test Content");
    }

    [Fact]
    public void HasCorrectPanelStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        cut.Find(".settings-panel").Should().NotBeNull();
        cut.Find(".settings-tabs-container").Should().NotBeNull();
        cut.Find(".settings-tabs").Should().NotBeNull();
        cut.Find(".settings-content").Should().NotBeNull();
    }

    [Fact]
    public void RendersTabGroups_WithCorrectHeaders()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        var groupHeaders = cut.FindAll(".tab-group-header");
        groupHeaders.Should().HaveCount(2);
        groupHeaders[0].TextContent.Should().Be("Configuration");
        groupHeaders[1].TextContent.Should().Be("Utilities");
    }

    [Fact]
    public void ConfigurationGroup_ContainsFiveTabs()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        var tabGroups = cut.FindAll(".tab-group");
        var configurationGroup = tabGroups[0];
        var tabs = configurationGroup.QuerySelectorAll(".settings-tab-item");
        tabs.Length.Should().Be(6); // General, Agents, Research Teams, Inference Engines, MCP Servers, Speech
    }

    [Fact]
    public void UtilitiesGroup_ContainsOneTab()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        var tabGroups = cut.FindAll(".tab-group");
        var utilitiesGroup = tabGroups[1];
        var tabs = utilitiesGroup.QuerySelectorAll(".settings-tab-item");
        tabs.Length.Should().Be(1);
    }

    [Fact]
    public void SettingsFooter_ContainsWizardButton()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabs>(parameters => parameters
            .Add(p => p.SelectedTab, "general"));

        // Assert
        var footer = cut.Find(".settings-footer");
        footer.Should().NotBeNull();
        footer.InnerHtml.Should().Contain("Run Setup Wizard");
    }
}
