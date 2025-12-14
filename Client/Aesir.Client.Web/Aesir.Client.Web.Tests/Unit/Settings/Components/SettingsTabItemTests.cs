using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Settings.Components;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class SettingsTabItemTests : TestContext
{
    public SettingsTabItemTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void RendersLabel_Correctly()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "General")
            .Add(p => p.TabId, "general"));

        // Assert
        cut.Markup.Should().Contain("General");
    }

    [Fact]
    public void RendersIcon_WhenProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "Settings")
            .Add(p => p.TabId, "settings")
            .Add(p => p.Icon, "Settings"));

        // Assert
        cut.FindAll("svg").Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Tune")]
    [InlineData("Memory")]
    [InlineData("Dns")]
    [InlineData("Build")]
    [InlineData("SmartToy")]
    [InlineData("Timeline")]
    public void RendersCorrectIcon_ForIconName(string iconName)
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "Test")
            .Add(p => p.TabId, "test")
            .Add(p => p.Icon, iconName));

        // Assert
        cut.Find(".settings-tab-item").Should().NotBeNull();
    }

    [Fact]
    public void AppliesSelectedClass_WhenIsSelectedTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "General")
            .Add(p => p.TabId, "general")
            .Add(p => p.IsSelected, true));

        // Assert
        cut.Find(".settings-tab-item").ClassList.Should().Contain("selected");
    }

    [Fact]
    public void DoesNotApplySelectedClass_WhenIsSelectedFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "General")
            .Add(p => p.TabId, "general")
            .Add(p => p.IsSelected, false));

        // Assert
        cut.Find(".settings-tab-item").ClassList.Should().NotContain("selected");
    }

    [Fact]
    public async Task InvokesOnClick_WhenClicked()
    {
        // Arrange
        string? clickedTabId = null;
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "Agents")
            .Add(p => p.TabId, "agents")
            .Add(p => p.OnClick, EventCallback.Factory.Create<string>(this, (tabId) => clickedTabId = tabId)));

        // Act
        await cut.Find(".settings-tab-item").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        clickedTabId.Should().Be("agents");
    }

    [Fact]
    public void DefaultsToSettingsIcon_WhenUnknownIconProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "Test")
            .Add(p => p.TabId, "test")
            .Add(p => p.Icon, "UnknownIcon"));

        // Assert - should render without errors
        cut.Find(".settings-tab-item").Should().NotBeNull();
    }

    [Fact]
    public void HasCorrectStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsTabItem>(parameters => parameters
            .Add(p => p.Label, "General")
            .Add(p => p.TabId, "general"));

        // Assert
        cut.Find(".tab-label").Should().NotBeNull();
        cut.Find(".tab-icon").Should().NotBeNull();
    }
}
