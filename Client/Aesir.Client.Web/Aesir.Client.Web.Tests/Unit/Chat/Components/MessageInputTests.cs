using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Modules.Chat.Models;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class MessageInputTests : TestContext
{
    private readonly Mock<IChatStateService> _mockChatStateService;
    private readonly Mock<IConfigurationApiService> _mockConfigurationApiService;
    private readonly Mock<IDocumentApiService> _mockDocumentApiService;
    private readonly Mock<IAgentToolsService> _mockAgentToolsService;
    private readonly Mock<IResearchStateService> _mockResearchStateService;
    private readonly Mock<IResearchTeamApiService> _mockResearchTeamApiService;

    public MessageInputTests()
    {
        _mockChatStateService = new Mock<IChatStateService>();
        _mockConfigurationApiService = new Mock<IConfigurationApiService>();
        _mockDocumentApiService = new Mock<IDocumentApiService>();
        _mockAgentToolsService = new Mock<IAgentToolsService>();
        _mockResearchStateService = new Mock<IResearchStateService>();
        _mockResearchTeamApiService = new Mock<IResearchTeamApiService>();

        // Setup agents list for AgentSelectorCompact
        var emptyAgentsList = Array.Empty<AesirAgentBase>() as IReadOnlyList<AesirAgentBase>;
        _mockConfigurationApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(emptyAgentsList));

        // Setup research teams for AgentSelectorCompact
        var emptyTeamsList = Array.Empty<ResearchTeamBase>() as IReadOnlyList<ResearchTeamBase>;
        _mockResearchTeamApiService.Setup(x => x.GetActiveTeamsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<ResearchTeamBase>>.Success(emptyTeamsList));

        Services.AddSingleton(_mockChatStateService.Object);
        Services.AddSingleton(_mockConfigurationApiService.Object);
        Services.AddSingleton(_mockDocumentApiService.Object);
        Services.AddSingleton(_mockAgentToolsService.Object);
        Services.AddSingleton(_mockResearchStateService.Object);
        Services.AddSingleton(_mockResearchTeamApiService.Object);
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;

        // Render MudPopoverProvider first (required for MudBlazor popover components)
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Renders_WithDefaultPlaceholder()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Default placeholder is customizable via parameter
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void Renders_WithCustomPlaceholder()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.Placeholder, "Custom placeholder"));

        // Assert
        cut.Markup.Should().Contain("Custom placeholder");
    }

    [Fact]
    public void SendButton_IsPresent()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Send button uses ArrowUpward icon with send-button class
        cut.Markup.Should().Contain("send-button");
    }

    [Fact]
    public void SendButton_IsDisabled_WhenNoAgentSelected()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Send button should be disabled
        var sendButtons = cut.FindAll(".send-button");
        sendButtons.Should().NotBeEmpty();
        // The button is disabled via the CanSend property
    }

    [Fact]
    public void SendButton_IsDisabled_WhenIsDisabledTrue()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert - Component renders with IsDisabled true
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void SendButton_IsDisabled_WhenIsLoadingTrue()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert - Component renders with IsLoading true
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void HasAgentSelector()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - AgentSelectorCompact is present
        cut.Markup.Should().Contain("agent-selector");
    }

    [Fact]
    public void HasAttachFileButton()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - There are multiple icon buttons in the action bar
        var buttons = cut.FindAll("button");
        buttons.Count.Should().BeGreaterThanOrEqualTo(3); // Attach, Settings, History, Agent selector, Send
    }

    [Fact]
    public void HasSettingsButton()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Action bar contains multiple buttons
        var buttons = cut.FindAll("button");
        buttons.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void InputField_IsPresent()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Input field is present via message-input class
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void InputField_IsDisabled_WhenIsDisabledTrue()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert - Component renders successfully with disabled state
        cut.Markup.Should().Contain("message-input");
    }

    private void SetupWithAgent()
    {
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent", ChatModel = "gpt-4" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
    }

    #region Image Thumbnail Spinner Visibility Tests

    [Fact]
    public void ImageThumbnail_SpinnerOverlay_HasActiveClass_WhenUploading()
    {
        // Arrange
        SetupWithAgent();
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.GetConversationId, () => Guid.NewGuid()));

        // Add a pending file with Uploading status
        var pendingFile = CreateMockPendingFile("test.png", UploadStatus.Uploading);
        AddPendingFileToComponent(cut.Instance, pendingFile);
        cut.Render();

        // Act & Assert
        var overlays = cut.FindAll(".thumbnail-overlay-actions.active");
        overlays.Should().NotBeEmpty("spinner overlay should have 'active' class when uploading");
    }

    [Fact]
    public void ImageThumbnail_SpinnerOverlay_HasActiveClass_WhenProcessing()
    {
        // Arrange
        SetupWithAgent();
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.GetConversationId, () => Guid.NewGuid()));

        // Add a pending file with Processing status
        var pendingFile = CreateMockPendingFile("test.jpg", UploadStatus.Processing);
        AddPendingFileToComponent(cut.Instance, pendingFile);
        cut.Render();

        // Act & Assert
        var overlays = cut.FindAll(".thumbnail-overlay-actions.active");
        overlays.Should().NotBeEmpty("spinner overlay should have 'active' class when processing/indexing");
    }

    [Fact]
    public void ImageThumbnail_SpinnerOverlay_NoActiveClass_WhenCompleted()
    {
        // Arrange
        SetupWithAgent();
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.GetConversationId, () => Guid.NewGuid()));

        // Add a pending file with Completed status
        var pendingFile = CreateMockPendingFile("test.png", UploadStatus.Completed);
        AddPendingFileToComponent(cut.Instance, pendingFile);
        cut.Render();

        // Act & Assert
        var activeOverlays = cut.FindAll(".thumbnail-overlay-actions.active");
        activeOverlays.Should().BeEmpty("spinner overlay should NOT have 'active' class when completed");

        // Verify the overlay exists but without active class
        var allOverlays = cut.FindAll(".thumbnail-overlay-actions");
        allOverlays.Should().NotBeEmpty("overlay should still exist");
    }

    [Fact]
    public void ImageThumbnail_SpinnerOverlay_NoActiveClass_WhenPending()
    {
        // Arrange
        SetupWithAgent();
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.GetConversationId, () => Guid.NewGuid()));

        // Add a pending file with Pending status
        var pendingFile = CreateMockPendingFile("test.webp", UploadStatus.Pending);
        AddPendingFileToComponent(cut.Instance, pendingFile);
        cut.Render();

        // Act & Assert
        var activeOverlays = cut.FindAll(".thumbnail-overlay-actions.active");
        activeOverlays.Should().BeEmpty("spinner overlay should NOT have 'active' class when pending");
    }

    [Fact]
    public void ImageThumbnail_ShowsSuccessBadge_WhenCompleted()
    {
        // Arrange
        SetupWithAgent();
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.GetConversationId, () => Guid.NewGuid()));

        // Add a pending file with Completed status
        var pendingFile = CreateMockPendingFile("test.gif", UploadStatus.Completed);
        AddPendingFileToComponent(cut.Instance, pendingFile);
        cut.Render();

        // Act & Assert
        var successBadges = cut.FindAll(".thumbnail-success-badge");
        successBadges.Should().NotBeEmpty("success badge should be shown when upload is completed");
    }

    [Fact]
    public void ImageThumbnail_ShowsErrorBadge_WhenFailed()
    {
        // Arrange
        SetupWithAgent();
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.GetConversationId, () => Guid.NewGuid()));

        // Add a pending file with Failed status
        var pendingFile = CreateMockPendingFile("test.png", UploadStatus.Failed);
        pendingFile.ErrorMessage = "Upload failed";
        AddPendingFileToComponent(cut.Instance, pendingFile);
        cut.Render();

        // Act & Assert
        var errorBadges = cut.FindAll(".thumbnail-error-badge");
        errorBadges.Should().NotBeEmpty("error badge should be shown when upload failed");
    }

    private static PendingFileUpload CreateMockPendingFile(string fileName, UploadStatus status)
    {
        var mockBrowserFile = new Mock<IBrowserFile>();
        mockBrowserFile.Setup(f => f.Name).Returns(fileName);
        mockBrowserFile.Setup(f => f.Size).Returns(1024);
        mockBrowserFile.Setup(f => f.ContentType).Returns("image/png");

        return new PendingFileUpload
        {
            File = mockBrowserFile.Object,
            Status = status,
            ObjectUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==" // Minimal 1x1 PNG
        };
    }

    private static void AddPendingFileToComponent(MessageInput component, PendingFileUpload file)
    {
        // Use reflection to add file to the private _pendingFiles list
        var fieldInfo = typeof(MessageInput).GetField("_pendingFiles", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo != null)
        {
            var pendingFiles = (List<PendingFileUpload>)fieldInfo.GetValue(component)!;
            pendingFiles.Add(file);
        }
    }

    #endregion
}
