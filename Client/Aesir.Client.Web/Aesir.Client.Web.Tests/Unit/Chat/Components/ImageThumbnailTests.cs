using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ImageThumbnailTests : TestContext
{
    public ImageThumbnailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Basic Rendering

    [Fact]
    public void Renders_ImageThumbnail_Container()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png"));

        // Assert
        cut.Markup.Should().Contain("image-thumbnail");
    }

    [Fact]
    public void Renders_Image_WhenImageUrlProvided()
    {
        // Arrange
        var imageUrl = "https://example.com/image.png";

        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, imageUrl));

        // Assert
        cut.Markup.Should().Contain("<img");
        cut.Markup.Should().Contain($"src=\"{imageUrl}\"");
        cut.Markup.Should().Contain("thumbnail-image");
    }

    [Fact]
    public void Renders_Image_WhenFileObjectUrlProvided()
    {
        // Arrange
        var fileObjectUrl = "blob:https://localhost/12345";

        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.FileObjectUrl, fileObjectUrl));

        // Assert
        cut.Markup.Should().Contain("<img");
        cut.Markup.Should().Contain($"src=\"{fileObjectUrl}\"");
    }

    [Fact]
    public void PreferFileObjectUrl_OverImageUrl()
    {
        // Arrange
        var imageUrl = "https://example.com/image.png";
        var fileObjectUrl = "blob:https://localhost/12345";

        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, imageUrl)
            .Add(p => p.FileObjectUrl, fileObjectUrl));

        // Assert
        cut.Markup.Should().Contain($"src=\"{fileObjectUrl}\"");
        cut.Markup.Should().NotContain($"src=\"{imageUrl}\"");
    }

    [Fact]
    public void HasAltAttribute_WithFileName()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "photo.jpg")
            .Add(p => p.ImageUrl, "https://example.com/photo.jpg"));

        // Assert
        cut.Markup.Should().Contain("alt=\"photo.jpg\"");
    }

    #endregion

    #region Fallback Rendering

    [Fact]
    public void Renders_Fallback_WhenNoUrlProvided()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png"));

        // Assert
        cut.Markup.Should().Contain("thumbnail-fallback");
        cut.Markup.Should().NotContain("<img");
    }

    [Fact]
    public void Renders_Fallback_WhenImageUrlIsEmpty()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, ""));

        // Assert
        cut.Markup.Should().Contain("thumbnail-fallback");
    }

    [Fact]
    public void Renders_FileTypeIcon_InFallback()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png"));

        // Assert
        cut.Markup.Should().Contain("thumbnail-fallback");
        // Icon should be present (MudIcon)
        cut.Markup.Should().Contain("mud-icon-root");
    }

    #endregion

    #region Size Variants

    [Fact]
    public void Renders_SmallSize_WhenSpecified()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.Size, ImageThumbnail.ThumbnailSize.Small));

        // Assert
        cut.Markup.Should().Contain("size-small");
    }

    [Fact]
    public void Renders_MediumSize_ByDefault()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png"));

        // Assert
        cut.Markup.Should().Contain("size-medium");
    }

    [Fact]
    public void Renders_LargeSize_WhenSpecified()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.Size, ImageThumbnail.ThumbnailSize.Large));

        // Assert
        cut.Markup.Should().Contain("size-large");
    }

    [Fact]
    public void Renders_AutoSize_WhenSpecified()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.Size, ImageThumbnail.ThumbnailSize.Auto));

        // Assert
        cut.Markup.Should().Contain("size-auto");
    }

    #endregion

    #region Overlay

    [Fact]
    public void Renders_Overlay_WhenShowOverlayIsTrue_AndOnClickHasDelegate()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.ShowOverlay, true)
            .Add(p => p.OnClick, () => { }));

        // Assert
        cut.Markup.Should().Contain("thumbnail-overlay");
    }

    [Fact]
    public void DoesNotRender_Overlay_WhenShowOverlayIsFalse()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.ShowOverlay, false)
            .Add(p => p.OnClick, () => { }));

        // Assert - The overlay element should not be in the DOM at all
        // (component only renders overlay when ShowOverlay && OnClick.HasDelegate)
        var overlayElement = cut.FindAll(".thumbnail-overlay");
        overlayElement.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_Overlay_WhenNoOnClickDelegate()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.ShowOverlay, true));

        // Assert - the overlay div element should not be in the DOM
        var overlayElements = cut.FindAll("div.thumbnail-overlay");
        overlayElements.Should().BeEmpty();
    }

    #endregion

    #region Clickable Behavior

    [Fact]
    public void IsClickable_WhenOnClickHasDelegate()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.OnClick, () => { }));

        // Assert
        cut.Markup.Should().Contain("role=\"button\"");
        cut.Markup.Should().Contain("tabindex=\"0\"");
    }

    [Fact]
    public void IsNotClickable_WhenNoOnClickDelegate()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png"));

        // Assert
        cut.Markup.Should().Contain("tabindex=\"-1\"");
    }

    [Fact]
    public async Task InvokesOnClick_WhenClicked()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ImageUrl, "https://example.com/image.png")
            .Add(p => p.OnClick, () => clicked = true));

        // Act
        await cut.Find(".image-thumbnail").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        clicked.Should().BeTrue();
    }

    #endregion

    #region Error State

    [Fact]
    public void HasErrorClass_WhenNoImageProvided()
    {
        // Act
        var cut = RenderComponent<ImageThumbnail>(parameters => parameters
            .Add(p => p.FileName, "image.png"));

        // Assert
        cut.Markup.Should().Contain("error");
    }

    #endregion
}
