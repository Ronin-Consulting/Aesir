using Aesir.Client.Web.Modules.Settings.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

/// <summary>
/// Tests for the ModelSelector component.
/// Note: Component rendering tests for MudSelect are complex due to popover requirements.
/// These tests focus on the static helper method and model behavior.
/// </summary>
public class ModelSelectorTests
{
    #region HasThinkingSupport Static Method Tests

    [Fact]
    public void HasThinkingSupport_ReturnsTrue_WhenModelHasThinkingCapability()
    {
        // Arrange
        var model = CreateModelWithCapabilities("thinking");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsTrue_WhenModelHasReasoningCapability()
    {
        // Arrange
        var model = CreateModelWithCapabilities("reasoning");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsTrue_WhenModelHasExtendedThinkingCapability()
    {
        // Arrange
        var model = CreateModelWithCapabilities("extended-thinking");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsTrue_WhenModelHasChainOfThoughtCapability()
    {
        // Arrange
        var model = CreateModelWithCapabilities("chain-of-thought");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsFalse_WhenModelHasNoCapabilities()
    {
        // Arrange
        var model = new AesirModelInfo { Id = "test-model" };

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsFalse_WhenModelHasNullDetails()
    {
        // Arrange
        var model = new AesirModelInfo
        {
            Id = "test-model",
            Details = null
        };

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsFalse_WhenModelHasNullCapabilities()
    {
        // Arrange
        var model = new AesirModelInfo
        {
            Id = "test-model",
            Details = new AesirModelDetails { Capabilities = null }
        };

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsFalse_WhenModelHasEmptyCapabilities()
    {
        // Arrange
        var model = new AesirModelInfo
        {
            Id = "test-model",
            Details = new AesirModelDetails { Capabilities = [] }
        };

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsFalse_WhenModelHasOtherCapabilities()
    {
        // Arrange
        var model = CreateModelWithCapabilities("vision", "tool-use", "embedding");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasThinkingSupport_IsCaseInsensitive()
    {
        // Arrange
        var model = CreateModelWithCapabilities("THINKING", "Reasoning");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasThinkingSupport_ReturnsTrueForMixedCapabilities()
    {
        // Arrange
        var model = CreateModelWithCapabilities("vision", "thinking", "embedding");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasThinkingSupport_MatchesExactCapabilityNames()
    {
        // Arrange - "think" is not exact match for "thinking"
        var model = CreateModelWithCapabilities("think", "reason");

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("thinking")]
    [InlineData("Thinking")]
    [InlineData("THINKING")]
    [InlineData("reasoning")]
    [InlineData("Reasoning")]
    [InlineData("REASONING")]
    [InlineData("extended-thinking")]
    [InlineData("Extended-Thinking")]
    [InlineData("chain-of-thought")]
    [InlineData("Chain-Of-Thought")]
    public void HasThinkingSupport_RecognizesAllValidCapabilities(string capability)
    {
        // Arrange
        var model = CreateModelWithCapabilities(capability);

        // Act
        var result = ModelSelector.HasThinkingSupport(model);

        // Assert
        result.Should().BeTrue($"'{capability}' should be recognized as a thinking capability");
    }

    #endregion

    #region Helper Methods

    private static AesirModelInfo CreateModelWithCapabilities(params string[] capabilities)
    {
        return new AesirModelInfo
        {
            Id = "test-model",
            OwnedBy = "test",
            Details = new AesirModelDetails
            {
                Capabilities = capabilities
            }
        };
    }

    #endregion
}
