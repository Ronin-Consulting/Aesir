using System.ComponentModel.DataAnnotations;
using Aesir.Common.Models;
using Aesir.Modules.Logging.Models;

namespace Aesir.Modules.Logging.Tests.Models;

public class KernelLogFilterRequestTests
{
    #region Default Values Tests

    [Fact]
    public void KernelLogFilterRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var filter = new KernelLogFilterRequest();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.SortDirection.Should().Be(SortDirection.Descending);
    }

    #endregion

    #region Page Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void KernelLogFilterRequest_InvalidPage_FailsValidation(int page)
    {
        // Arrange
        var filter = new KernelLogFilterRequest { Page = page };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Page"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(1000)]
    public void KernelLogFilterRequest_ValidPage_PassesValidation(int page)
    {
        // Arrange
        var filter = new KernelLogFilterRequest { Page = page };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Page"));
    }

    #endregion

    #region PageSize Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    [InlineData(500)]
    public void KernelLogFilterRequest_InvalidPageSize_FailsValidation(int pageSize)
    {
        // Arrange
        var filter = new KernelLogFilterRequest { PageSize = pageSize };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("PageSize"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void KernelLogFilterRequest_ValidPageSize_PassesValidation(int pageSize)
    {
        // Arrange
        var filter = new KernelLogFilterRequest { PageSize = pageSize };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("PageSize"));
    }

    #endregion

    #region SortDirection Tests

    [Fact]
    public void SortDirection_Ascending_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest { SortDirection = SortDirection.Ascending };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void SortDirection_Descending_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest { SortDirection = SortDirection.Descending };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Optional Filter Tests

    [Fact]
    public void KernelLogFilterRequest_WithTimeRange_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-7),
            To = DateTimeOffset.UtcNow
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void KernelLogFilterRequest_WithLogLevels_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            Levels = [KernelLogLevel.Error, KernelLogLevel.Warning, KernelLogLevel.Info]
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.Levels.Should().HaveCount(3);
    }

    [Fact]
    public void KernelLogFilterRequest_WithLogTypes_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            Types = [KernelLogType.FunctionInvocation, KernelLogType.AutoFunctionInvocation, KernelLogType.PromptRender]
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.Types.Should().HaveCount(3);
    }

    [Fact]
    public void KernelLogFilterRequest_WithChatSessionId_IsValid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filter = new KernelLogFilterRequest
        {
            ChatSessionId = sessionId
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.ChatSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void KernelLogFilterRequest_WithConversationId_IsValid()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var filter = new KernelLogFilterRequest
        {
            ConversationId = conversationId
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public void KernelLogFilterRequest_WithFunctionNameSearch_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            FunctionName = "test_function"
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.FunctionName.Should().Be("test_function");
    }

    [Fact]
    public void KernelLogFilterRequest_WithPluginNameSearch_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            PluginName = "test_plugin"
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.PluginName.Should().Be("test_plugin");
    }

    [Fact]
    public void KernelLogFilterRequest_WithMessageSearch_IsValid()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            MessageSearch = "error occurred"
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
        filter.MessageSearch.Should().Be("error occurred");
    }

    [Fact]
    public void KernelLogFilterRequest_WithAllFilters_IsValid()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var filter = new KernelLogFilterRequest
        {
            Page = 1,
            PageSize = 100,
            From = DateTimeOffset.UtcNow.AddDays(-30),
            To = DateTimeOffset.UtcNow,
            ChatSessionId = chatSessionId,
            ConversationId = conversationId,
            Levels = [KernelLogLevel.Error],
            Types = [KernelLogType.FunctionInvocation],
            FunctionName = "search",
            PluginName = "documents",
            MessageSearch = "executed",
            SortDirection = SortDirection.Ascending
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    #endregion
}
