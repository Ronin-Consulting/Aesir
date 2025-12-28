using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using FluentAssertions;
using Xunit;

namespace Aesir.Modules.Research.Tests.Services;

public class ScoringCalculatorTests
{
    private readonly ScoringCalculator _calculator;

    public ScoringCalculatorTests()
    {
        _calculator = new ScoringCalculator();
    }

    #region CalculateWeightedAverage Tests

    [Fact]
    public void CalculateWeightedAverage_WithAllTens_ReturnsTen()
    {
        // Act
        var result = _calculator.CalculateWeightedAverage(10, 10, 10, 10, 10);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void CalculateWeightedAverage_WithAllOnes_ReturnsOne()
    {
        // Act
        var result = _calculator.CalculateWeightedAverage(1, 1, 1, 1, 1);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void CalculateWeightedAverage_WithMixedScores_ReturnsCorrectWeightedValue()
    {
        // Arrange - weights: depth=0.25, accuracy=0.25, source_quality=0.20, novelty=0.15, coherence=0.15
        var depth = 8.0;
        var accuracy = 7.0;
        var sourceQuality = 6.0;
        var novelty = 9.0;
        var coherence = 5.0;

        // Expected: 8*0.25 + 7*0.25 + 6*0.20 + 9*0.15 + 5*0.15 = 2 + 1.75 + 1.2 + 1.35 + 0.75 = 7.05

        // Act
        var result = _calculator.CalculateWeightedAverage(depth, accuracy, sourceQuality, novelty, coherence);

        // Assert
        result.Should().Be(7.05);
    }

    [Fact]
    public void CalculateWeightedAverage_ClampsValuesBelowOne()
    {
        // Act
        var result = _calculator.CalculateWeightedAverage(0, -5, 1, 1, 1);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void CalculateWeightedAverage_ClampsValuesAboveTen()
    {
        // Act
        var result = _calculator.CalculateWeightedAverage(15, 20, 10, 10, 10);

        // Assert
        result.Should().BeLessThanOrEqualTo(10);
    }

    [Theory]
    [InlineData(5, 5, 5, 5, 5, 5.0)]
    [InlineData(10, 1, 10, 1, 10, 6.4)]   // 10*0.25 + 1*0.25 + 10*0.20 + 1*0.15 + 10*0.15 = 6.4
    [InlineData(1, 10, 1, 10, 1, 4.6)]    // 1*0.25 + 10*0.25 + 1*0.20 + 10*0.15 + 1*0.15 = 4.6
    public void CalculateWeightedAverage_VariousInputs_ReturnsExpectedValues(
        double depth, double accuracy, double sourceQuality, double novelty, double coherence, double expected)
    {
        // Act
        var result = _calculator.CalculateWeightedAverage(depth, accuracy, sourceQuality, novelty, coherence);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region CalculateAggregateScore Tests

    [Fact]
    public void CalculateAggregateScore_WithNoReviews_ReturnsDefaultScore()
    {
        // Arrange
        var reviews = new List<PeerReview>();

        // Act
        var result = _calculator.CalculateAggregateScore(reviews);

        // Assert
        result.AverageScore.Should().Be(0);
        result.MedianScore.Should().Be(0);
        result.ReviewCount.Should().Be(0);
        result.Confidence.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void CalculateAggregateScore_WithSingleReview_ReturnsReviewScore()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var reviews = new List<PeerReview>
        {
            CreatePeerReview(submissionId, 8, 7, 6, 5, 4, weightedAverage: 6.35, endorses: true)
        };

        // Act
        var result = _calculator.CalculateAggregateScore(reviews);

        // Assert
        result.SubmissionId.Should().Be(submissionId);
        result.AverageScore.Should().Be(6.35);
        result.MedianScore.Should().Be(6.35);
        result.ReviewCount.Should().Be(1);
        result.EndorsementCount.Should().Be(1);
    }

    [Fact]
    public void CalculateAggregateScore_WithMultipleReviews_CalculatesStatistics()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var reviews = new List<PeerReview>
        {
            CreatePeerReview(submissionId, 8, 8, 8, 8, 8, weightedAverage: 8.0, endorses: true),
            CreatePeerReview(submissionId, 6, 6, 6, 6, 6, weightedAverage: 6.0, endorses: true),
            CreatePeerReview(submissionId, 7, 7, 7, 7, 7, weightedAverage: 7.0, endorses: false)
        };

        // Act
        var result = _calculator.CalculateAggregateScore(reviews);

        // Assert
        result.SubmissionId.Should().Be(submissionId);
        result.AverageScore.Should().Be(7.0);
        result.MedianScore.Should().Be(7.0);
        result.MinScore.Should().Be(6.0);
        result.MaxScore.Should().Be(8.0);
        result.ReviewCount.Should().Be(3);
        result.EndorsementCount.Should().Be(2);
        result.StandardDeviation.Should().Be(1.0);
    }

    [Fact]
    public void CalculateAggregateScore_WithEvenReviews_CalculatesMedianCorrectly()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var reviews = new List<PeerReview>
        {
            CreatePeerReview(submissionId, 5, 5, 5, 5, 5, weightedAverage: 5.0, endorses: false),
            CreatePeerReview(submissionId, 6, 6, 6, 6, 6, weightedAverage: 6.0, endorses: false),
            CreatePeerReview(submissionId, 7, 7, 7, 7, 7, weightedAverage: 7.0, endorses: true),
            CreatePeerReview(submissionId, 8, 8, 8, 8, 8, weightedAverage: 8.0, endorses: true)
        };

        // Act
        var result = _calculator.CalculateAggregateScore(reviews);

        // Assert - median of 5,6,7,8 = (6+7)/2 = 6.5
        result.MedianScore.Should().Be(6.5);
    }

    [Fact]
    public void CalculateAggregateScore_CalculatesCriterionAverages()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var reviews = new List<PeerReview>
        {
            CreatePeerReview(submissionId, 8, 6, 7, 5, 9, weightedAverage: 7.0, endorses: true),
            CreatePeerReview(submissionId, 6, 8, 5, 7, 7, weightedAverage: 6.6, endorses: true)
        };

        // Act
        var result = _calculator.CalculateAggregateScore(reviews);

        // Assert
        result.CriterionAverages.Should().ContainKey("depth");
        result.CriterionAverages["depth"].Should().Be(7.0); // (8+6)/2
        result.CriterionAverages["accuracy"].Should().Be(7.0); // (6+8)/2
        result.CriterionAverages["source_quality"].Should().Be(6.0); // (7+5)/2
        result.CriterionAverages["novelty"].Should().Be(6.0); // (5+7)/2
        result.CriterionAverages["coherence"].Should().Be(8.0); // (9+7)/2
    }

    #endregion

    #region CalculateConfidence Tests

    [Fact]
    public void CalculateConfidence_WithSingleScore_ReturnsLow()
    {
        // Arrange
        var scores = new List<double> { 8.0 };

        // Act
        var result = _calculator.CalculateConfidence(scores);

        // Assert
        result.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void CalculateConfidence_WithLowVariance_ReturnsHigh()
    {
        // Arrange - scores with stddev <= 1.0
        var scores = new List<double> { 7.5, 7.8, 7.2, 7.6 };

        // Act
        var result = _calculator.CalculateConfidence(scores);

        // Assert
        result.Should().Be(ConfidenceLevel.High);
    }

    [Fact]
    public void CalculateConfidence_WithMediumVariance_ReturnsMedium()
    {
        // Arrange - scores with stddev between 1.0 and 2.0
        var scores = new List<double> { 5.0, 7.0, 6.0, 8.0 };

        // Act
        var result = _calculator.CalculateConfidence(scores);

        // Assert
        result.Should().Be(ConfidenceLevel.Medium);
    }

    [Fact]
    public void CalculateConfidence_WithHighVariance_ReturnsLow()
    {
        // Arrange - scores with stddev > 2.0
        var scores = new List<double> { 2.0, 9.0, 3.0, 8.0 };

        // Act
        var result = _calculator.CalculateConfidence(scores);

        // Assert
        result.Should().Be(ConfidenceLevel.Low);
    }

    #endregion

    #region GetCriterionWeight Tests

    [Theory]
    [InlineData("depth", 0.25)]
    [InlineData("accuracy", 0.25)]
    [InlineData("source_quality", 0.20)]
    [InlineData("novelty", 0.15)]
    [InlineData("coherence", 0.15)]
    public void GetCriterionWeight_ReturnsExpectedWeight(string criterion, double expectedWeight)
    {
        // Act
        var result = _calculator.GetCriterionWeight(criterion);

        // Assert
        result.Should().Be(expectedWeight);
    }

    [Fact]
    public void GetCriterionWeight_IsCaseInsensitive()
    {
        // Act
        var lowerResult = _calculator.GetCriterionWeight("depth");
        var upperResult = _calculator.GetCriterionWeight("DEPTH");
        var mixedResult = _calculator.GetCriterionWeight("DePtH");

        // Assert
        lowerResult.Should().Be(0.25);
        upperResult.Should().Be(0.25);
        mixedResult.Should().Be(0.25);
    }

    [Fact]
    public void GetCriterionWeight_UnknownCriterion_ReturnsZero()
    {
        // Act
        var result = _calculator.GetCriterionWeight("unknown_criterion");

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private static PeerReview CreatePeerReview(
        Guid submissionId,
        double depth,
        double accuracy,
        double sourceQuality,
        double novelty,
        double coherence,
        double weightedAverage,
        bool endorses)
    {
        return new PeerReview
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            ReviewerAgentId = Guid.NewGuid(),
            ScoreDepth = depth,
            ScoreAccuracy = accuracy,
            ScoreSourceQuality = sourceQuality,
            ScoreNovelty = novelty,
            ScoreCoherence = coherence,
            WeightedAverage = weightedAverage,
            Endorses = endorses,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
