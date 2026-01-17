using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Tests.Models;

public class ResearchEnumsTests
{
    #region ResearchRole Tests

    [Fact]
    public void ResearchRole_HasExpectedValues()
    {
        // Assert
        Enum.GetNames<ResearchRole>().Should().Contain("DeepDiver");
        Enum.GetNames<ResearchRole>().Should().Contain("Synthesizer");
        Enum.GetNames<ResearchRole>().Should().Contain("DevilsAdvocate");
        Enum.GetNames<ResearchRole>().Should().Contain("Chairman");
    }

    [Fact]
    public void ResearchRole_HasCorrectCount()
    {
        // Assert
        Enum.GetValues<ResearchRole>().Should().HaveCount(4);
    }

    #endregion

    #region ResearchMode Tests

    [Fact]
    public void ResearchMode_HasExpectedValues()
    {
        // Assert
        Enum.GetNames<ResearchMode>().Should().Contain("Quick");
        Enum.GetNames<ResearchMode>().Should().Contain("Standard");
        Enum.GetNames<ResearchMode>().Should().Contain("Deep");
    }

    [Fact]
    public void ResearchMode_HasCorrectCount()
    {
        // Assert
        Enum.GetValues<ResearchMode>().Should().HaveCount(3);
    }

    #endregion

    #region ResearchStatus Tests

    [Fact]
    public void ResearchStatus_HasExpectedValues()
    {
        // Assert
        var statuses = Enum.GetNames<ResearchStatus>();
        statuses.Should().Contain("Created");
        statuses.Should().Contain("AwaitingClarification");
        statuses.Should().Contain("Planning");
        statuses.Should().Contain("Researching");
        statuses.Should().Contain("Anonymizing");
        statuses.Should().Contain("PeerReviewing");
        statuses.Should().Contain("Synthesizing");
        statuses.Should().Contain("Completed");
        statuses.Should().Contain("Failed");
        statuses.Should().Contain("Cancelled");
    }

    [Fact]
    public void ResearchStatus_HasCorrectCount()
    {
        // Assert
        Enum.GetValues<ResearchStatus>().Should().HaveCount(10);
    }

    [Theory]
    [InlineData(ResearchStatus.Created)]
    [InlineData(ResearchStatus.AwaitingClarification)]
    [InlineData(ResearchStatus.Planning)]
    [InlineData(ResearchStatus.Researching)]
    [InlineData(ResearchStatus.Anonymizing)]
    [InlineData(ResearchStatus.PeerReviewing)]
    [InlineData(ResearchStatus.Synthesizing)]
    public void ResearchStatus_InProgressStatuses_AreNotTerminal(ResearchStatus status)
    {
        // Assert - these statuses are not terminal states
        status.Should().NotBe(ResearchStatus.Completed);
        status.Should().NotBe(ResearchStatus.Failed);
        status.Should().NotBe(ResearchStatus.Cancelled);
    }

    [Theory]
    [InlineData(ResearchStatus.Completed)]
    [InlineData(ResearchStatus.Failed)]
    [InlineData(ResearchStatus.Cancelled)]
    public void ResearchStatus_TerminalStatuses_AreRecognized(ResearchStatus status)
    {
        // Assert - terminal statuses should exist
        Enum.IsDefined(status).Should().BeTrue();
    }

    #endregion

    #region ResearchPhase Tests

    [Fact]
    public void ResearchPhase_HasExpectedValues()
    {
        // Assert
        var phases = Enum.GetNames<ResearchPhase>();
        phases.Should().Contain("Clarification");
        phases.Should().Contain("Planning");
        phases.Should().Contain("Research");
        phases.Should().Contain("Anonymization");
        phases.Should().Contain("PeerReview");
        phases.Should().Contain("Synthesis");
    }

    [Fact]
    public void ResearchPhase_HasCorrectCount()
    {
        // Assert
        Enum.GetValues<ResearchPhase>().Should().HaveCount(6);
    }

    [Fact]
    public void ResearchPhase_FollowsExpectedWorkflowOrder()
    {
        // The workflow should follow this order
        ((int)ResearchPhase.Clarification).Should().BeLessThan((int)ResearchPhase.Planning);
        ((int)ResearchPhase.Planning).Should().BeLessThan((int)ResearchPhase.Research);
        ((int)ResearchPhase.Research).Should().BeLessThan((int)ResearchPhase.Anonymization);
        ((int)ResearchPhase.Anonymization).Should().BeLessThan((int)ResearchPhase.PeerReview);
        ((int)ResearchPhase.PeerReview).Should().BeLessThan((int)ResearchPhase.Synthesis);
    }

    #endregion

    #region SubmissionStatus Tests

    [Fact]
    public void SubmissionStatus_HasExpectedValues()
    {
        // Assert
        var statuses = Enum.GetNames<SubmissionStatus>();
        statuses.Should().Contain("Pending");
        statuses.Should().Contain("Planning");
        statuses.Should().Contain("Researching");
        statuses.Should().Contain("Completed");
        statuses.Should().Contain("Failed");
    }

    [Fact]
    public void SubmissionStatus_HasCorrectCount()
    {
        // Assert
        Enum.GetValues<SubmissionStatus>().Should().HaveCount(5);
    }

    #endregion

    #region ConfidenceLevel Tests

    [Fact]
    public void ConfidenceLevel_HasExpectedValues()
    {
        // Assert
        Enum.GetNames<ConfidenceLevel>().Should().Contain("Low");
        Enum.GetNames<ConfidenceLevel>().Should().Contain("Medium");
        Enum.GetNames<ConfidenceLevel>().Should().Contain("High");
    }

    [Fact]
    public void ConfidenceLevel_HasCorrectCount()
    {
        // Assert
        Enum.GetValues<ConfidenceLevel>().Should().HaveCount(3);
    }

    [Fact]
    public void ConfidenceLevel_FollowsCorrectOrder()
    {
        // Low should be less than Medium, Medium less than High
        ((int)ConfidenceLevel.Low).Should().BeLessThan((int)ConfidenceLevel.Medium);
        ((int)ConfidenceLevel.Medium).Should().BeLessThan((int)ConfidenceLevel.High);
    }

    #endregion
}
