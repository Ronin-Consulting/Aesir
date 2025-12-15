using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class AesirProcessingIndicatorTests : TestContext
{
    public AesirProcessingIndicatorTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_ProcessingIndicator()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>();

        // Assert
        cut.Markup.Should().Contain("aesir-processing");
    }

    [Fact]
    public void WhenProcessing_ShouldShowStatusText()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>(parameters => parameters
            .Add(p => p.IsProcessing, true));

        // Assert
        cut.Markup.Should().Contain("status-text");
        cut.Markup.Should().Contain("shimmer-text");
    }

    [Fact]
    public void WhenNotProcessing_ShouldNotShowStatusText()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>(parameters => parameters
            .Add(p => p.IsProcessing, false));

        // Assert
        cut.FindAll(".status-text").Should().BeEmpty();
    }

    [Fact]
    public void WhenProcessing_ShouldHaveActiveClass()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>(parameters => parameters
            .Add(p => p.IsProcessing, true));

        // Assert
        cut.Markup.Should().Contain("active");
    }

    [Fact]
    public void WhenNotProcessing_ShouldNotHaveActiveClass()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>(parameters => parameters
            .Add(p => p.IsProcessing, false));

        // Assert
        cut.FindAll(".aesir-icon.active").Should().BeEmpty();
    }

    [Fact]
    public void ShouldRenderBrandIconImage()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>();

        // Assert
        cut.Markup.Should().Contain("brand-icon-animated");
        cut.Markup.Should().Contain("logo-blue.svg");
    }

    [Fact]
    public void WhenCustomStatus_ShouldShowCustomText()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>(parameters => parameters
            .Add(p => p.IsProcessing, true)
            .Add(p => p.CustomStatus, "Custom processing..."));

        // Assert
        cut.Markup.Should().Contain("Custom processing...");
    }

    [Fact]
    public void ShouldHaveIconContainer()
    {
        // Act
        var cut = RenderComponent<AesirProcessingIndicator>();

        // Assert
        cut.Markup.Should().Contain("aesir-icon");
    }
}
