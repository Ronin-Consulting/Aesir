using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using FluentAssertions;
using Xunit;

namespace Aesir.Modules.Research.Tests.Services;

public class ConfidenceCalculatorTests
{
    private readonly ConfidenceCalculator _calculator;

    public ConfidenceCalculatorTests()
    {
        _calculator = new ConfidenceCalculator();
    }

    #region CalculateFindingConfidence Tests

    [Fact]
    public void CalculateFindingConfidence_WithNoScores_ReturnsLow()
    {
        // Arrange
        var scores = new List<SubmissionScore>();

        // Act
        var result = _calculator.CalculateFindingConfidence(scores, 0.5, 3);

        // Assert
        result.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void CalculateFindingConfidence_WithHighScores_ReturnsHigh()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(8.5, ConfidenceLevel.High),
            CreateSubmissionScore(9.0, ConfidenceLevel.High)
        };

        // Act - high score (8+), high endorsement (0.9+), high sources (5+)
        var result = _calculator.CalculateFindingConfidence(scores, 0.95, 6);

        // Assert
        result.Should().Be(ConfidenceLevel.High);
    }

    [Fact]
    public void CalculateFindingConfidence_WithMediumScores_ReturnsMedium()
    {
        // Arrange - To get Medium (>= 4 points):
        // avgScore >= 7.0 → 2 pts, endorsementRate >= 0.7 → 1 pt, sourceCount >= 2 → 1 pt, consensus Medium → 1 pt = 5 pts
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(7.0, ConfidenceLevel.Medium),
            CreateSubmissionScore(7.0, ConfidenceLevel.Medium)
        };

        // Act
        var result = _calculator.CalculateFindingConfidence(scores, 0.7, 3);

        // Assert
        result.Should().Be(ConfidenceLevel.Medium);
    }

    [Fact]
    public void CalculateFindingConfidence_WithLowScores_ReturnsLow()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(3.0, ConfidenceLevel.Low),
            CreateSubmissionScore(4.0, ConfidenceLevel.Low)
        };

        // Act
        var result = _calculator.CalculateFindingConfidence(scores, 0.2, 0);

        // Assert
        result.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void CalculateFindingConfidence_SourceCountAffectsConfidence()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(7.0, ConfidenceLevel.Medium)
        };

        // Act - same scores but different source counts
        var resultWithManySources = _calculator.CalculateFindingConfidence(scores, 0.7, 5);
        var resultWithFewSources = _calculator.CalculateFindingConfidence(scores, 0.7, 1);

        // Assert - more sources should give higher confidence
        resultWithManySources.Should().BeOneOf(ConfidenceLevel.High, ConfidenceLevel.Medium);
        resultWithFewSources.Should().BeOneOf(ConfidenceLevel.Medium, ConfidenceLevel.Low);
    }

    #endregion

    #region CalculateOverallConfidence Tests

    [Fact]
    public void CalculateOverallConfidence_WithNoScores_ReturnsLow()
    {
        // Arrange
        var scores = new List<SubmissionScore>();
        var reviews = new List<PeerReview>();

        // Act
        var result = _calculator.CalculateOverallConfidence(scores, reviews);

        // Assert
        result.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void CalculateOverallConfidence_WithHighScoresAndEndorsements_ReturnsHigh()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(8.0, ConfidenceLevel.High),
            CreateSubmissionScore(8.5, ConfidenceLevel.High),
            CreateSubmissionScore(9.0, ConfidenceLevel.Medium) // 2/3 high = 66% > 50%
        };

        var reviews = new List<PeerReview>
        {
            CreatePeerReview(endorses: true),
            CreatePeerReview(endorses: true),
            CreatePeerReview(endorses: true),
            CreatePeerReview(endorses: true),
            CreatePeerReview(endorses: false) // 80% endorsement
        };

        // Act
        var result = _calculator.CalculateOverallConfidence(scores, reviews);

        // Assert
        result.Should().Be(ConfidenceLevel.High);
    }

    [Fact]
    public void CalculateOverallConfidence_WithMediumMetrics_ReturnsMedium()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(6.0, ConfidenceLevel.Medium),
            CreateSubmissionScore(6.5, ConfidenceLevel.Medium)
        };

        var reviews = new List<PeerReview>
        {
            CreatePeerReview(endorses: true),
            CreatePeerReview(endorses: false) // 50% endorsement
        };

        // Act
        var result = _calculator.CalculateOverallConfidence(scores, reviews);

        // Assert
        result.Should().Be(ConfidenceLevel.Medium);
    }

    [Fact]
    public void CalculateOverallConfidence_WithLowEndorsement_ReturnsLow()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(5.0, ConfidenceLevel.Low)
        };

        var reviews = new List<PeerReview>
        {
            CreatePeerReview(endorses: false),
            CreatePeerReview(endorses: false),
            CreatePeerReview(endorses: false)
        };

        // Act
        var result = _calculator.CalculateOverallConfidence(scores, reviews);

        // Assert
        result.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void CalculateOverallConfidence_WithNoReviews_UsesZeroEndorsementRate()
    {
        // Arrange
        var scores = new List<SubmissionScore>
        {
            CreateSubmissionScore(7.0, ConfidenceLevel.Medium)
        };

        var reviews = new List<PeerReview>();

        // Act
        var result = _calculator.CalculateOverallConfidence(scores, reviews);

        // Assert - without reviews, endorsement rate is 0, so shouldn't be High
        result.Should().NotBe(ConfidenceLevel.High);
    }

    #endregion

    #region ScoreToConfidence Tests

    [Theory]
    [InlineData(10.0, ConfidenceLevel.High)]
    [InlineData(8.5, ConfidenceLevel.High)]
    [InlineData(7.5, ConfidenceLevel.High)]
    [InlineData(7.0, ConfidenceLevel.Medium)]
    [InlineData(6.0, ConfidenceLevel.Medium)]
    [InlineData(5.5, ConfidenceLevel.Medium)]
    [InlineData(5.0, ConfidenceLevel.Low)]
    [InlineData(3.0, ConfidenceLevel.Low)]
    [InlineData(1.0, ConfidenceLevel.Low)]
    public void ScoreToConfidence_ReturnsExpectedLevel(double score, ConfidenceLevel expected)
    {
        // Act
        var result = _calculator.ScoreToConfidence(score);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ScoreToConfidence_AtBoundaryHigh_ReturnsHigh()
    {
        // Act - 7.5 is the boundary for high
        var result = _calculator.ScoreToConfidence(7.5);

        // Assert
        result.Should().Be(ConfidenceLevel.High);
    }

    [Fact]
    public void ScoreToConfidence_JustBelowHigh_ReturnsMedium()
    {
        // Act
        var result = _calculator.ScoreToConfidence(7.49);

        // Assert
        result.Should().Be(ConfidenceLevel.Medium);
    }

    #endregion

    #region GetConfidenceExplanation Tests

    [Fact]
    public void GetConfidenceExplanation_HighConfidence_IncludesStrengths()
    {
        // Arrange
        var factors = new ConfidenceFactors
        {
            AverageScore = 8.5,
            EndorsementRate = 0.9,
            SourceCount = 5,
            DevilsAdvocateEndorsed = true
        };

        // Act
        var result = _calculator.GetConfidenceExplanation(ConfidenceLevel.High, factors);

        // Assert
        result.Should().Contain("Strong peer review scores");
        result.Should().Contain("High endorsement rate");
        result.Should().Contain("Well-sourced");
        result.Should().Contain("Validated by critical analysis");
    }

    [Fact]
    public void GetConfidenceExplanation_MediumConfidence_IncludesModerations()
    {
        // Arrange
        var factors = new ConfidenceFactors
        {
            AverageScore = 6.5,
            ScoreVariance = 2.5,
            SourceCount = 2
        };

        // Act
        var result = _calculator.GetConfidenceExplanation(ConfidenceLevel.Medium, factors);

        // Assert
        result.Should().Contain("Moderate peer review scores");
        result.Should().Contain("disagreement");
        result.Should().Contain("Limited source coverage");
    }

    [Fact]
    public void GetConfidenceExplanation_LowConfidence_IncludesWeaknesses()
    {
        // Arrange
        var factors = new ConfidenceFactors
        {
            AverageScore = 4.0,
            EndorsementRate = 0.2,
            ScoreVariance = 4.0,
            SourceCount = 0
        };

        // Act
        var result = _calculator.GetConfidenceExplanation(ConfidenceLevel.Low, factors);

        // Assert
        result.Should().Contain("Lower peer review scores");
        result.Should().Contain("Low endorsement rate");
        result.Should().Contain("Significant disagreement");
        result.Should().Contain("No sources cited");
    }

    [Fact]
    public void GetConfidenceExplanation_WithMinimalFactors_ReturnsDefaultMessage()
    {
        // Arrange
        var factors = new ConfidenceFactors
        {
            AverageScore = 6.0,
            EndorsementRate = 0.6,
            ScoreVariance = 1.5,
            SourceCount = 3
        };

        // Act
        var result = _calculator.GetConfidenceExplanation(ConfidenceLevel.Medium, factors);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private static SubmissionScore CreateSubmissionScore(double averageScore, ConfidenceLevel confidence)
    {
        return new SubmissionScore
        {
            SubmissionId = Guid.NewGuid(),
            AverageScore = averageScore,
            Confidence = confidence,
            ReviewCount = 3,
            EndorsementCount = 2
        };
    }

    private static PeerReview CreatePeerReview(bool endorses)
    {
        return new PeerReview
        {
            Id = Guid.NewGuid(),
            SubmissionId = Guid.NewGuid(),
            ReviewerAgentId = Guid.NewGuid(),
            ScoreDepth = 7.0,
            ScoreAccuracy = 7.0,
            ScoreSourceQuality = 7.0,
            ScoreNovelty = 7.0,
            ScoreCoherence = 7.0,
            WeightedAverage = 7.0,
            Endorses = endorses,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
