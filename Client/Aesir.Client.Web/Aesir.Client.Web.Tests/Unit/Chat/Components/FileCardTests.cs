using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class FileCardTests : TestContext
{
    public FileCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Basic Rendering

    [Fact]
    public void Renders_FileCard_WithFileName()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf"));

        // Assert
        cut.Markup.Should().Contain("file-card");
        cut.Markup.Should().Contain("document.pdf");
    }

    [Fact]
    public void Renders_FileCard_WithTruncatedFileName()
    {
        // Arrange - filename longer than default truncation length
        var longFileName = "this-is-a-very-long-filename-that-needs-truncation.pdf";

        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, longFileName));

        // Assert
        cut.Markup.Should().Contain("...");
        cut.Markup.Should().Contain(".pdf");
    }

    [Fact]
    public void Renders_FileSize_WhenShowFileSizeIsTrue()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.FileSize, 1048576) // 1 MB
            .Add(p => p.ShowFileSize, true));

        // Assert
        cut.Markup.Should().Contain("1 MB");
    }

    [Fact]
    public void DoesNotRender_FileSize_WhenShowFileSizeIsFalse()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.FileSize, 1048576)
            .Add(p => p.ShowFileSize, false));

        // Assert
        cut.Markup.Should().NotContain("1 MB");
    }

    [Fact]
    public void Renders_SecondaryText_WhenProvided()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.SecondaryText, "Page 5"));

        // Assert
        cut.Markup.Should().Contain("Page 5");
    }

    [Fact]
    public void Renders_SecondaryText_InsteadOfFileSize()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.FileSize, 1048576)
            .Add(p => p.ShowFileSize, true)
            .Add(p => p.SecondaryText, "Custom text"));

        // Assert
        cut.Markup.Should().Contain("Custom text");
        cut.Markup.Should().NotContain("1 MB");
    }

    #endregion

    #region Size Variants

    [Fact]
    public void Renders_CompactSize_WhenSpecified()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.Size, FileCard.FileCardSize.Compact));

        // Assert
        cut.Markup.Should().Contain("size-compact");
    }

    [Fact]
    public void Renders_DefaultSize_WhenSpecified()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.Size, FileCard.FileCardSize.Default));

        // Assert
        cut.Markup.Should().Contain("size-default");
    }

    [Fact]
    public void Renders_LargeSize_WhenSpecified()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.Size, FileCard.FileCardSize.Large));

        // Assert
        cut.Markup.Should().Contain("size-large");
    }

    #endregion

    #region State Classes

    [Fact]
    public void Renders_UploadingState_WhenIsUploadingIsTrue()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsUploading, true));

        // Assert
        cut.Markup.Should().Contain("state-uploading");
    }

    [Fact]
    public void Renders_ErrorState_WhenHasErrorIsTrue()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.HasError, true));

        // Assert
        cut.Markup.Should().Contain("state-error");
    }

    [Fact]
    public void Renders_DeletedState_WhenIsDeletedIsTrue()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsDeleted, true));

        // Assert
        cut.Markup.Should().Contain("state-deleted");
    }

    [Fact]
    public void Renders_SuccessState_WhenShowSuccessIsTrue()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.ShowSuccess, true));

        // Assert
        cut.Markup.Should().Contain("state-success");
        cut.Markup.Should().Contain("success-badge");
    }

    [Fact]
    public void Renders_RemovingClass_WhenIsRemovingIsTrue()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsRemoving, true));

        // Assert
        cut.Markup.Should().Contain("removing");
    }

    #endregion

    #region Upload Progress

    [Fact]
    public void Renders_ProgressOverlay_WhenUploading()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsUploading, true)
            .Add(p => p.Progress, 50));

        // Assert
        cut.Markup.Should().Contain("upload-overlay");
        cut.Markup.Should().Contain("progress-bar");
    }

    [Fact]
    public void DoesNotRender_ProgressBar_WhenProgressIsZero()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsUploading, true)
            .Add(p => p.Progress, 0));

        // Assert - The progress-bar element should not be in the DOM when progress is 0
        var progressBarElement = cut.FindAll(".progress-bar");
        progressBarElement.Should().BeEmpty();
    }

    #endregion

    #region Error Display

    [Fact]
    public void Renders_ErrorBadge_WhenHasError()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.HasError, true));

        // Assert
        cut.Markup.Should().Contain("error-badge");
    }

    [Fact]
    public void Renders_ErrorMessage_WhenProvided()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.HasError, true)
            .Add(p => p.ErrorMessage, "Upload failed"));

        // Assert
        cut.Markup.Should().Contain("Upload failed");
        cut.Markup.Should().Contain("error-text");
    }

    #endregion

    #region Thumbnail

    [Fact]
    public void Renders_Thumbnail_WhenThumbnailUrlProvided()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ThumbnailUrl, "https://example.com/thumb.png")
            .Add(p => p.ShowThumbnail, true));

        // Assert
        cut.Markup.Should().Contain("<img");
        cut.Markup.Should().Contain("file-thumbnail");
    }

    [Fact]
    public void DoesNotRender_Thumbnail_WhenShowThumbnailIsFalse()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "image.png")
            .Add(p => p.ThumbnailUrl, "https://example.com/thumb.png")
            .Add(p => p.ShowThumbnail, false));

        // Assert - there should be no img element with file-thumbnail class
        var thumbnailElements = cut.FindAll("img.file-thumbnail");
        thumbnailElements.Should().BeEmpty();
    }

    #endregion

    #region Actions

    [Fact]
    public void Renders_RemoveButton_WhenOnRemoveHasDelegate()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.OnRemove, () => { }));

        // Assert
        cut.Markup.Should().Contain("file-actions");
        cut.Markup.Should().Contain("remove-btn");
        cut.Markup.Should().Contain("aria-label=\"Remove file\"");
    }

    [Fact]
    public void Renders_DownloadButton_WhenOnDownloadHasDelegate()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.OnDownload, () => { }));

        // Assert
        cut.Markup.Should().Contain("file-actions");
        cut.Markup.Should().Contain("aria-label=\"Download file\"");
    }

    [Fact]
    public void DoesNotRender_DownloadButton_WhenUploading()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsUploading, true)
            .Add(p => p.OnDownload, () => { }));

        // Assert - there should be no download button with that aria-label
        var downloadButtons = cut.FindAll("button[aria-label='Download file']");
        downloadButtons.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_Actions_WhenNoCallbacksProvided()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf"));

        // Assert - there should be no file-actions div element
        var actionElements = cut.FindAll("div.file-actions");
        actionElements.Should().BeEmpty();
    }

    #endregion

    #region Clickable Behavior

    [Fact]
    public void IsClickable_WhenOnClickHasDelegate()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.OnClick, () => { }));

        // Assert
        cut.Markup.Should().Contain("clickable");
        cut.Markup.Should().Contain("role=\"button\"");
        cut.Markup.Should().Contain("tabindex=\"0\"");
    }

    [Fact]
    public void IsNotClickable_WhenOnClickIsNull()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf"));

        // Assert - when no OnClick delegate, the file card should not have clickable class in its class attribute
        var fileCard = cut.Find(".file-card");
        var classAttr = fileCard.GetAttribute("class");
        classAttr.Should().NotContain("clickable");
    }

    [Fact]
    public void IsNotClickable_WhenIsUploading()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsUploading, true)
            .Add(p => p.OnClick, () => { }));

        // Assert - when uploading, the file card should not have clickable class
        var fileCard = cut.Find(".file-card");
        var classAttr = fileCard.GetAttribute("class");
        classAttr.Should().NotContain("clickable");
    }

    [Fact]
    public void IsNotClickable_WhenIsDeleted()
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsDeleted, true)
            .Add(p => p.OnClick, () => { }));

        // Assert - when deleted, the file card should not have clickable class
        var fileCard = cut.Find(".file-card");
        var classAttr = fileCard.GetAttribute("class");
        classAttr.Should().NotContain("clickable");
    }

    [Fact]
    public async Task InvokesOnClick_WhenClicked()
    {
        // Arrange
        var clicked = false;

        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.OnClick, () => clicked = true));

        // Act
        await cut.Find(".file-card").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        clicked.Should().BeTrue();
    }

    [Fact]
    public async Task InvokesOnRemove_WhenRemoveButtonClicked()
    {
        // Arrange
        var removed = false;

        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.OnRemove, () => removed = true));

        // Act
        await cut.Find(".remove-btn").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        removed.Should().BeTrue();
    }

    #endregion

    #region File Type Icons

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("image.png")]
    [InlineData("script.js")]
    [InlineData("data.json")]
    public void Renders_CorrectIconWrapper_ForFileType(string fileName)
    {
        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, fileName));

        // Assert
        cut.Markup.Should().Contain("file-icon-wrapper");
    }

    #endregion

    #region Title Attribute

    [Fact]
    public void HasTitleAttribute_WithFullFileName()
    {
        // Arrange
        var fileName = "document.pdf";

        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, fileName));

        // Assert
        cut.Markup.Should().Contain($"title=\"{fileName}\"");
    }

    [Fact]
    public void HasTitleAttribute_WithLongFileName()
    {
        // Arrange
        var longFileName = "this-is-a-very-long-filename-that-gets-truncated.pdf";

        // Act
        var cut = RenderComponent<FileCard>(parameters => parameters
            .Add(p => p.FileName, longFileName));

        // Assert - full filename should be in title even if display is truncated
        cut.Markup.Should().Contain($"title=\"{longFileName}\"");
    }

    #endregion
}
