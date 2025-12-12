using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class ToolCallStateServiceTests
{
    private readonly ToolCallStateService _sut;

    public ToolCallStateServiceTests()
    {
        _sut = new ToolCallStateService();
    }

    #region ProcessToolCallEvent Tests

    [Fact]
    public void ProcessToolCallEvent_AddsNewToolCall()
    {
        // Arrange
        var toolCall = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);

        // Act
        _sut.ProcessToolCallEvent(toolCall);

        // Assert
        _sut.CurrentToolCalls.Should().HaveCount(1);
        _sut.CurrentToolCalls[0].ToolCallId.Should().Be("tc-1");
        _sut.CurrentToolCalls[0].FunctionName.Should().Be("TestFunction");
    }

    [Fact]
    public void ProcessToolCallEvent_UpdatesExistingToolCall()
    {
        // Arrange
        var startEvent = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);
        var completeEvent = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Completed);
        completeEvent.Result = "Success result";
        completeEvent.CompletedAt = DateTimeOffset.UtcNow;

        // Act
        _sut.ProcessToolCallEvent(startEvent);
        _sut.ProcessToolCallEvent(completeEvent);

        // Assert
        _sut.CurrentToolCalls.Should().HaveCount(1);
        _sut.CurrentToolCalls[0].Status.Should().Be(ToolCallStatus.Completed);
        _sut.CurrentToolCalls[0].Result.Should().Be("Success result");
    }

    [Fact]
    public void ProcessToolCallEvent_MaintainsOrderForMultipleToolCalls()
    {
        // Arrange
        var tc1 = CreateToolCall("tc-1", "FirstFunction", ToolCallStatus.Started);
        var tc2 = CreateToolCall("tc-2", "SecondFunction", ToolCallStatus.Started);
        var tc3 = CreateToolCall("tc-3", "ThirdFunction", ToolCallStatus.Started);

        // Act
        _sut.ProcessToolCallEvent(tc1);
        _sut.ProcessToolCallEvent(tc2);
        _sut.ProcessToolCallEvent(tc3);

        // Assert
        _sut.CurrentToolCalls.Should().HaveCount(3);
        _sut.CurrentToolCalls[0].FunctionName.Should().Be("FirstFunction");
        _sut.CurrentToolCalls[1].FunctionName.Should().Be("SecondFunction");
        _sut.CurrentToolCalls[2].FunctionName.Should().Be("ThirdFunction");
    }

    [Fact]
    public void ProcessToolCallEvent_ThrowsOnNullToolCall()
    {
        // Act & Assert
        var act = () => _sut.ProcessToolCallEvent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ProcessToolCallEvent_FiresOnToolCallsChangedEvent()
    {
        // Arrange
        var eventFired = false;
        _sut.OnToolCallsChanged += () => eventFired = true;
        var toolCall = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);

        // Act
        _sut.ProcessToolCallEvent(toolCall);

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ProcessToolCallEvent_UpdatesError_WhenToolCallFails()
    {
        // Arrange
        var startEvent = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);
        var failEvent = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Failed);
        failEvent.Error = "Something went wrong";

        // Act
        _sut.ProcessToolCallEvent(startEvent);
        _sut.ProcessToolCallEvent(failEvent);

        // Assert
        _sut.CurrentToolCalls[0].Status.Should().Be(ToolCallStatus.Failed);
        _sut.CurrentToolCalls[0].Error.Should().Be("Something went wrong");
    }

    #endregion

    #region HasActiveToolCalls Tests

    [Fact]
    public void HasActiveToolCalls_ReturnsTrue_WhenStartedToolCallExists()
    {
        // Arrange
        var toolCall = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);
        _sut.ProcessToolCallEvent(toolCall);

        // Act & Assert
        _sut.HasActiveToolCalls.Should().BeTrue();
    }

    [Fact]
    public void HasActiveToolCalls_ReturnsFalse_WhenAllToolCallsCompleted()
    {
        // Arrange
        var startEvent = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);
        var completeEvent = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Completed);
        _sut.ProcessToolCallEvent(startEvent);
        _sut.ProcessToolCallEvent(completeEvent);

        // Act & Assert
        _sut.HasActiveToolCalls.Should().BeFalse();
    }

    [Fact]
    public void HasActiveToolCalls_ReturnsFalse_WhenNoToolCalls()
    {
        // Act & Assert
        _sut.HasActiveToolCalls.Should().BeFalse();
    }

    #endregion

    #region GetStatusCounts Tests

    [Fact]
    public void GetStatusCounts_ReturnsCorrectCounts()
    {
        // Arrange
        var tc1 = CreateToolCall("tc-1", "Func1", ToolCallStatus.Started);
        var tc2 = CreateToolCall("tc-2", "Func2", ToolCallStatus.Completed);
        var tc3 = CreateToolCall("tc-3", "Func3", ToolCallStatus.Failed);
        var tc4 = CreateToolCall("tc-4", "Func4", ToolCallStatus.Completed);

        _sut.ProcessToolCallEvent(tc1);
        _sut.ProcessToolCallEvent(tc2);
        _sut.ProcessToolCallEvent(tc3);
        _sut.ProcessToolCallEvent(tc4);

        // Act
        var (total, completed, failed, inProgress) = _sut.GetStatusCounts();

        // Assert
        total.Should().Be(4);
        completed.Should().Be(2);
        failed.Should().Be(1);
        inProgress.Should().Be(1);
    }

    [Fact]
    public void GetStatusCounts_ReturnsZeros_WhenEmpty()
    {
        // Act
        var (total, completed, failed, inProgress) = _sut.GetStatusCounts();

        // Assert
        total.Should().Be(0);
        completed.Should().Be(0);
        failed.Should().Be(0);
        inProgress.Should().Be(0);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllToolCalls()
    {
        // Arrange
        _sut.ProcessToolCallEvent(CreateToolCall("tc-1", "Func1", ToolCallStatus.Started));
        _sut.ProcessToolCallEvent(CreateToolCall("tc-2", "Func2", ToolCallStatus.Completed));

        // Act
        _sut.Clear();

        // Assert
        _sut.CurrentToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void Clear_FiresOnToolCallsChangedEvent()
    {
        // Arrange
        _sut.ProcessToolCallEvent(CreateToolCall("tc-1", "Func1", ToolCallStatus.Started));
        var eventFired = false;
        _sut.OnToolCallsChanged += () => eventFired = true;

        // Act
        _sut.Clear();

        // Assert
        eventFired.Should().BeTrue();
    }

    #endregion

    #region GetToolCall Tests

    [Fact]
    public void GetToolCall_ReturnsToolCall_WhenExists()
    {
        // Arrange
        var toolCall = CreateToolCall("tc-1", "TestFunction", ToolCallStatus.Started);
        _sut.ProcessToolCallEvent(toolCall);

        // Act
        var result = _sut.GetToolCall("tc-1");

        // Assert
        result.Should().NotBeNull();
        result!.FunctionName.Should().Be("TestFunction");
    }

    [Fact]
    public void GetToolCall_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = _sut.GetToolCall("non-existent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ProcessToolCallEvent_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() =>
                _sut.ProcessToolCallEvent(CreateToolCall($"tc-{id}", $"Func{id}", ToolCallStatus.Started))));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        _sut.CurrentToolCalls.Should().HaveCount(100);
    }

    #endregion

    #region Helper Methods

    private static AesirToolCallInfo CreateToolCall(string id, string functionName, ToolCallStatus status)
    {
        return new AesirToolCallInfo
        {
            ToolCallId = id,
            FunctionName = functionName,
            PluginName = "TestPlugin",
            Status = status,
            ToolType = ToolCallType.Other,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
