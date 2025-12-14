using Aesir.Client.Web.Modules.Observability.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Observability.Models;

public class LogFilterTests
{
    #region Default Values Tests

    [Fact]
    public void LogFilter_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var filter = new LogFilter();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.From.Should().BeNull();
        filter.To.Should().BeNull();
        filter.ChatSessionId.Should().BeNull();
        filter.ConversationId.Should().BeNull();
        filter.Levels.Should().BeEmpty();
        filter.Types.Should().BeEmpty();
        filter.FunctionName.Should().BeNull();
        filter.PluginName.Should().BeNull();
        filter.MessageSearch.Should().BeNull();
        filter.SortAscending.Should().BeFalse();
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new LogFilter
        {
            Page = 5,
            PageSize = 100,
            From = DateTimeOffset.Now.AddDays(-7),
            To = DateTimeOffset.Now,
            ChatSessionId = Guid.NewGuid(),
            Levels = [KernelLogLevel.Error, KernelLogLevel.Warning],
            Types = [KernelLogType.FunctionInvocation],
            FunctionName = "test_func",
            PluginName = "test_plugin"
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Page.Should().Be(original.Page);
        clone.PageSize.Should().Be(original.PageSize);
        clone.From.Should().Be(original.From);
        clone.To.Should().Be(original.To);
        clone.ChatSessionId.Should().Be(original.ChatSessionId);
        clone.Levels.Should().BeEquivalentTo(original.Levels);
        clone.Types.Should().BeEquivalentTo(original.Types);
        clone.FunctionName.Should().Be(original.FunctionName);
        clone.PluginName.Should().Be(original.PluginName);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new LogFilter
        {
            Levels = [KernelLogLevel.Error],
            Types = [KernelLogType.FunctionInvocation]
        };

        // Act
        var clone = original.Clone();
        clone.Levels.Add(KernelLogLevel.Warning);
        clone.Types.Add(KernelLogType.PromptRender);
        clone.Page = 10;

        // Assert
        original.Levels.Should().HaveCount(1);
        original.Levels.Should().Contain(KernelLogLevel.Error);
        original.Types.Should().HaveCount(1);
        original.Page.Should().Be(1);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllValues()
    {
        // Arrange
        var filter = new LogFilter
        {
            Page = 5,
            PageSize = 100,
            From = DateTimeOffset.Now.AddDays(-7),
            To = DateTimeOffset.Now,
            ChatSessionId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Levels = [KernelLogLevel.Error],
            Types = [KernelLogType.FunctionInvocation],
            FunctionName = "test",
            PluginName = "plugin",
            MessageSearch = "search",
            SortAscending = true
        };

        // Act
        filter.Reset();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.From.Should().BeNull();
        filter.To.Should().BeNull();
        filter.ChatSessionId.Should().BeNull();
        filter.ConversationId.Should().BeNull();
        filter.Levels.Should().BeEmpty();
        filter.Types.Should().BeEmpty();
        filter.FunctionName.Should().BeNull();
        filter.PluginName.Should().BeNull();
        filter.MessageSearch.Should().BeNull();
        filter.SortAscending.Should().BeFalse();
    }

    #endregion

    #region ToQueryString Tests

    [Fact]
    public void ToQueryString_WithDefaultValues_IncludesRequiredParams()
    {
        // Arrange
        var filter = new LogFilter();

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("page=1");
        query.Should().Contain("pageSize=50");
        query.Should().Contain("sortDirection=Descending");
    }

    [Fact]
    public void ToQueryString_WithTimeRange_IncludesFromAndTo()
    {
        // Arrange
        var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var filter = new LogFilter
        {
            From = from,
            To = to
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("from=");
        query.Should().Contain("to=");
    }

    [Fact]
    public void ToQueryString_WithLevels_IncludesEachLevel()
    {
        // Arrange
        var filter = new LogFilter
        {
            Levels = [KernelLogLevel.Error, KernelLogLevel.Warning]
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("levels=Error");
        query.Should().Contain("levels=Warning");
    }

    [Fact]
    public void ToQueryString_WithTypes_IncludesEachType()
    {
        // Arrange
        var filter = new LogFilter
        {
            Types = [KernelLogType.FunctionInvocation, KernelLogType.PromptRender]
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("types=FunctionInvocation");
        query.Should().Contain("types=PromptRender");
    }

    [Fact]
    public void ToQueryString_WithChatSessionId_IncludesId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filter = new LogFilter
        {
            ChatSessionId = sessionId
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain($"chatSessionId={sessionId}");
    }

    [Fact]
    public void ToQueryString_WithFunctionName_IncludesEncodedName()
    {
        // Arrange
        var filter = new LogFilter
        {
            FunctionName = "test function"
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("functionName=test%20function");
    }

    [Fact]
    public void ToQueryString_WithSortAscending_IncludesAscending()
    {
        // Arrange
        var filter = new LogFilter
        {
            SortAscending = true
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("sortDirection=Ascending");
    }

    [Fact]
    public void ToQueryString_WithEmptyFunctionName_DoesNotIncludeFunctionName()
    {
        // Arrange
        var filter = new LogFilter
        {
            FunctionName = ""
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().NotContain("functionName=");
    }

    [Fact]
    public void ToQueryString_WithWhitespaceFunctionName_DoesNotIncludeFunctionName()
    {
        // Arrange
        var filter = new LogFilter
        {
            FunctionName = "   "
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().NotContain("functionName=");
    }

    #endregion
}
