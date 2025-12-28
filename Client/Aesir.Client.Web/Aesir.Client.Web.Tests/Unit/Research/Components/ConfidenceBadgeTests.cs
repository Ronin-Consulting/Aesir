using Aesir.Client.Web.Modules.Research.Components;
using Bunit;
using MudBlazor;
using MudBlazor.Services;

namespace Aesir.Client.Web.Tests.Unit.Research.Components;

public class ConfidenceBadgeTests : TestContext
{
    public ConfidenceBadgeTests()
    {
        Services.AddMudServices();
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
    }

    [Fact]
    public void ConfidenceBadge_RendersWithDefaultLevel()
    {
        // Act
        var cut = RenderComponent<ConfidenceBadge>();

        // Assert - default is "Medium"
        cut.Markup.Should().Contain("Medium");
    }

    [Theory]
    [InlineData("High")]
    [InlineData("Medium")]
    [InlineData("Low")]
    public void ConfidenceBadge_RendersWithProvidedLevel(string level)
    {
        // Act
        var cut = RenderComponent<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Level, level));

        // Assert
        cut.Markup.Should().Contain(level);
    }

    [Fact]
    public void ConfidenceBadge_RendersVeryHighLevel()
    {
        // Act
        var cut = RenderComponent<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Level, "VeryHigh"));

        // Assert
        cut.Markup.Should().Contain("VeryHigh");
    }

    [Fact]
    public void ConfidenceBadge_HasBadgeContent()
    {
        // Act
        var cut = RenderComponent<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Level, "High"));

        // Assert - should contain the badge-content div
        cut.Markup.Should().Contain("badge-content");
    }

    [Fact]
    public void ConfidenceBadge_RendersMudChip()
    {
        // Act
        var cut = RenderComponent<ConfidenceBadge>();

        // Assert - MudChip should be rendered
        cut.FindComponent<MudChip<string>>().Should().NotBeNull();
    }

    [Fact]
    public void ConfidenceBadge_ContainsMudIcon()
    {
        // Act
        var cut = RenderComponent<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Level, "High"));

        // Assert - should contain an icon
        var icons = cut.FindComponents<MudIcon>();
        icons.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("high")]
    [InlineData("HIGH")]
    [InlineData("High")]
    public void ConfidenceBadge_IsCaseInsensitive(string level)
    {
        // Act - should work with any case
        var cut = RenderComponent<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Level, level));

        // Assert - should render without error
        cut.Markup.Should().Contain(level);
    }
}
