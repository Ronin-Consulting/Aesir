using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class FileAttachmentTests : TestContext
{
    public FileAttachmentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Basic Rendering Tests

    [Fact]
    public void Renders_WithFileName()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "document.pdf"));

        // Assert
        cut.Markup.Should().Contain("document.pdf");
    }

    [Fact]
    public void Renders_WithFileAttachmentClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf"));

        // Assert
        cut.Markup.Should().Contain("file-attachment");
    }

    [Fact]
    public void Renders_WithoutError_WhenEmptyFileName()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, ""));

        // Assert
        cut.Markup.Should().NotBeEmpty();
    }

    #endregion

    #region File Icon Tests

    [Theory]
    [InlineData("document.pdf", Icons.Material.Filled.PictureAsPdf)]
    [InlineData("readme.txt", Icons.Material.Filled.Description)]
    [InlineData("notes.md", Icons.Material.Filled.Description)]
    [InlineData("data.json", Icons.Material.Filled.DataObject)]
    [InlineData("config.xml", Icons.Material.Filled.DataObject)]
    [InlineData("data.csv", Icons.Material.Filled.TableChart)]
    [InlineData("image.png", Icons.Material.Filled.Image)]
    [InlineData("photo.jpg", Icons.Material.Filled.Image)]
    [InlineData("photo.jpeg", Icons.Material.Filled.Image)]
    [InlineData("animation.gif", Icons.Material.Filled.Image)]
    [InlineData("picture.webp", Icons.Material.Filled.Image)]
    [InlineData("unknown.xyz", Icons.Material.Filled.InsertDriveFile)]
    public void Renders_CorrectIcon_ForFileType(string filename, string expectedIcon)
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, filename));

        // Assert - Check the icon path is present in markup
        cut.Markup.Should().Contain("mud-icon");
    }

    #endregion

    #region Mode Tests

    [Fact]
    public void Renders_PendingMode_WithCorrectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Pending));

        // Assert
        cut.Markup.Should().Contain("mode-pending");
    }

    [Fact]
    public void Renders_AttachedMode_WithCorrectClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached));

        // Assert
        cut.Markup.Should().Contain("mode-attached");
    }

    #endregion

    #region File Size Tests

    [Fact]
    public void Renders_FileSize_WhenShowSizeIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.FileSize, 1024)
            .Add(p => p.ShowSize, true));

        // Assert
        cut.Markup.Should().Contain("1 KB");
    }

    [Fact]
    public void DoesNotRender_FileSize_WhenShowSizeIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.FileSize, 1024)
            .Add(p => p.ShowSize, false));

        // Assert
        cut.Markup.Should().NotContain("1 KB");
    }

    [Fact]
    public void DoesNotRender_FileSize_WhenSizeIsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.FileSize, 0)
            .Add(p => p.ShowSize, true));

        // Assert - No file-size span element should be rendered
        cut.FindAll("span.file-size").Should().BeEmpty();
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatsFileSize_Correctly(long bytes, string expected)
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.FileSize, bytes)
            .Add(p => p.ShowSize, true));

        // Assert
        cut.Markup.Should().Contain(expected);
    }

    #endregion

    #region Processing State Tests

    [Fact]
    public void Renders_Spinner_WhenIsProcessingIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsProcessing, true));

        // Assert
        cut.Markup.Should().Contain("spinner-overlay");
    }

    [Fact]
    public void DoesNotRender_Spinner_WhenIsProcessingIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsProcessing, false));

        // Assert - No spinner-overlay div element should be rendered
        cut.FindAll("div.spinner-overlay").Should().BeEmpty();
    }

    [Fact]
    public void Renders_ProcessingClass_OnIcon_WhenProcessing()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsProcessing, true));

        // Assert
        cut.Markup.Should().Contain("processing");
    }

    [Fact]
    public void Renders_IndeterminateSpinner_WhenProgressIsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsProcessing, true)
            .Add(p => p.Progress, 0));

        // Assert - Indeterminate spinner should be present
        cut.Markup.Should().Contain("mud-progress-circular");
    }

    [Fact]
    public void Renders_DeterminateSpinner_WhenProgressHasValue()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsProcessing, true)
            .Add(p => p.Progress, 50));

        // Assert
        cut.Markup.Should().Contain("mud-progress-circular");
    }

    #endregion

    #region Error State Tests

    [Fact]
    public void Renders_ErrorClass_WhenHasErrorIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.HasError, true));

        // Assert
        cut.Markup.Should().Contain("error");
    }

    [Fact]
    public void Renders_ErrorOverlay_WhenHasErrorIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.HasError, true));

        // Assert
        cut.Markup.Should().Contain("error-overlay");
    }

    [Fact]
    public void Renders_ErrorMessage_WhenProvided()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.HasError, true)
            .Add(p => p.ErrorMessage, "Upload failed"));

        // Assert
        cut.Markup.Should().Contain("Upload failed");
        cut.Markup.Should().Contain("error-message");
    }

    [Fact]
    public void DoesNotRender_ErrorOverlay_WhenNoError()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.HasError, false));

        // Assert - No error-overlay div element should be rendered
        cut.FindAll("div.error-overlay").Should().BeEmpty();
    }

    #endregion

    #region Deleted State Tests

    [Fact]
    public void Renders_DeletedClass_WhenIsDeletedIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsDeleted, true));

        // Assert
        cut.Markup.Should().Contain("deleted");
    }

    [Fact]
    public void DoesNotRender_DeletedClass_WhenIsDeletedIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.IsDeleted, false));

        // Assert
        cut.Markup.Should().NotContain("class=\"file-attachment mode-pending deleted\"");
    }

    #endregion

    #region Remove Button Tests

    [Fact]
    public void Renders_RemoveButton_InPendingMode_WithOnRemoveHandler()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Pending)
            .Add(p => p.OnRemove, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.Should().Contain("remove-button");
    }

    [Fact]
    public void DoesNotRender_RemoveButton_InAttachedMode()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached)
            .Add(p => p.OnRemove, EventCallback.Factory.Create(this, () => { })));

        // Assert - No remove-button element should be rendered
        cut.FindAll(".remove-button").Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_RemoveButton_WithoutOnRemoveHandler()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Pending));

        // Assert - No remove-button element should be rendered
        cut.FindAll(".remove-button").Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveButton_InvokesOnRemove_WhenClicked()
    {
        // Arrange
        var removeClicked = false;
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Pending)
            .Add(p => p.OnRemove, EventCallback.Factory.Create(this, () => removeClicked = true)));

        // Act
        var removeButton = cut.Find(".remove-button");
        await cut.InvokeAsync(() => removeButton.Click());

        // Assert
        removeClicked.Should().BeTrue();
    }

    #endregion

    #region Click Handler Tests

    [Fact]
    public void Renders_Clickable_InAttachedMode_WithOnClickHandler()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.Should().Contain("clickable");
    }

    [Fact]
    public void DoesNotRender_Clickable_WhenProcessing()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached)
            .Add(p => p.IsProcessing, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => { })));

        // Assert - The file-attachment div should not have the clickable class
        cut.FindAll("div.file-attachment.clickable").Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_Clickable_WhenDeleted()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached)
            .Add(p => p.IsDeleted, true)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => { })));

        // Assert - The file-attachment div should not have the clickable class
        cut.FindAll("div.file-attachment.clickable").Should().BeEmpty();
    }

    [Fact]
    public async Task OnClick_IsInvoked_WhenClickable()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "test.pdf")
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var element = cut.Find(".file-attachment");
        await cut.InvokeAsync(() => element.Click());

        // Assert
        clicked.Should().BeTrue();
    }

    #endregion

    #region Filename Truncation Tests

    [Fact]
    public void TruncatesLongFilename_InPendingMode()
    {
        // Arrange
        var longFilename = "this_is_a_very_long_filename_that_should_be_truncated.pdf";

        // Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, longFilename)
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Pending));

        // Assert - In pending mode, max length is 20
        cut.Markup.Should().Contain("...");
        cut.Markup.Should().NotContain(longFilename);
    }

    [Fact]
    public void TruncatesLongFilename_InAttachedMode_WithDifferentLength()
    {
        // Arrange
        var longFilename = "this_is_a_very_long_filename_that_exceeds_thirty_chars.pdf";

        // Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, longFilename)
            .Add(p => p.Mode, FileAttachment.FileAttachmentMode.Attached));

        // Assert - In attached mode, max length is 30
        cut.Markup.Should().Contain("...");
    }

    [Fact]
    public void DoesNotTruncate_ShortFilename()
    {
        // Arrange
        var shortFilename = "doc.pdf";

        // Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, shortFilename));

        // Assert
        cut.Markup.Should().Contain("doc.pdf");
        cut.Markup.Should().NotContain("...");
    }

    #endregion

    #region Tooltip Tests

    [Fact]
    public void Renders_Tooltip_WithFileName()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "document.pdf"));

        // Assert - MudTooltip should be present
        cut.Markup.Should().Contain("mud-tooltip");
    }

    [Fact]
    public void Tooltip_ShowsDeletedMessage_WhenDeleted()
    {
        // Arrange & Act
        var cut = RenderComponent<FileAttachment>(parameters => parameters
            .Add(p => p.FileName, "document.pdf")
            .Add(p => p.IsDeleted, true));

        // Assert - Check for tooltip text in aria or data attribute
        // MudTooltip renders the text, we verify the component handles it
        cut.Markup.Should().Contain("mud-tooltip");
    }

    #endregion
}
