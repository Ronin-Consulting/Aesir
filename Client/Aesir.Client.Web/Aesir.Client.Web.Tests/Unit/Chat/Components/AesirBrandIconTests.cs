using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class AesirBrandIconTests : TestContext
{
    public AesirBrandIconTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_BrandIcon_WithDefaultSize()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>();

        // Assert
        cut.Markup.Should().Contain("aesir-brand");
        cut.Markup.Should().Contain("size-medium"); // Default size
    }

    [Fact]
    public void WhenShowReadyMessageTrue_ShouldDisplayReadyMessage()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>(parameters => parameters
            .Add(p => p.ShowReadyMessage, true));

        // Assert
        cut.Markup.Should().Contain("ready-message");
        cut.Markup.Should().Contain("with-message");
    }

    [Fact]
    public void WhenShowReadyMessageFalse_ShouldNotDisplayReadyMessage()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>(parameters => parameters
            .Add(p => p.ShowReadyMessage, false));

        // Assert
        cut.FindAll(".ready-message").Should().BeEmpty();
    }

    [Fact]
    public void ShouldApplySmallSizeClass()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>(parameters => parameters
            .Add(p => p.IconSize, AesirBrandIcon.BrandIconSize.Small));

        // Assert
        cut.Markup.Should().Contain("size-small");
    }

    [Fact]
    public void ShouldApplyMediumSizeClass()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>(parameters => parameters
            .Add(p => p.IconSize, AesirBrandIcon.BrandIconSize.Medium));

        // Assert
        cut.Markup.Should().Contain("size-medium");
    }

    [Fact]
    public void ShouldApplyLargeSizeClass()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>(parameters => parameters
            .Add(p => p.IconSize, AesirBrandIcon.BrandIconSize.Large));

        // Assert
        cut.Markup.Should().Contain("size-large");
    }

    [Fact]
    public void ShouldApplyExtraLargeSizeClass()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>(parameters => parameters
            .Add(p => p.IconSize, AesirBrandIcon.BrandIconSize.ExtraLarge));

        // Assert
        cut.Markup.Should().Contain("size-extra-large");
    }

    [Fact]
    public void ShouldRenderBrandIconImage()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>();

        // Assert
        cut.Markup.Should().Contain("brand-icon");
        cut.Markup.Should().Contain("logo-blue.svg");
    }

    [Fact]
    public void ShouldHaveContainerClass()
    {
        // Act
        var cut = RenderComponent<AesirBrandIcon>();

        // Assert
        cut.Markup.Should().Contain("aesir-brand-container");
    }
}
