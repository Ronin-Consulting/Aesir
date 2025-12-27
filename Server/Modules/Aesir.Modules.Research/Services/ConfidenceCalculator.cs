using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Calculates overall confidence levels for research findings based on
/// peer review scores, consensus, and source quality.
/// </summary>
public interface IConfidenceCalculator
{
    /// <summary>
    /// Calculates the confidence level for a finding based on peer reviews.
    /// </summary>
    /// <param name="submissionScores">Scores for submissions that contributed to this finding.</param>
    /// <param name="endorsementRate">Rate of endorsements (0-1).</param>
    /// <param name="sourceCount">Number of sources supporting the finding.</param>
    /// <returns>The calculated confidence level.</returns>
    ConfidenceLevel CalculateFindingConfidence(
        IReadOnlyList<SubmissionScore> submissionScores,
        double endorsementRate,
        int sourceCount);

    /// <summary>
    /// Calculates the overall confidence level for the research session.
    /// </summary>
    /// <param name="allScores">All submission scores.</param>
    /// <param name="peerReviews">All peer reviews.</param>
    /// <returns>The overall confidence level.</returns>
    ConfidenceLevel CalculateOverallConfidence(
        IReadOnlyList<SubmissionScore> allScores,
        IReadOnlyList<PeerReview> peerReviews);

    /// <summary>
    /// Gets a confidence level from a numeric score.
    /// </summary>
    /// <param name="score">Score from 0-10.</param>
    /// <returns>The corresponding confidence level.</returns>
    ConfidenceLevel ScoreToConfidence(double score);

    /// <summary>
    /// Gets a confidence explanation for display.
    /// </summary>
    /// <param name="confidence">The confidence level.</param>
    /// <param name="factors">Factors that influenced the calculation.</param>
    /// <returns>Human-readable explanation.</returns>
    string GetConfidenceExplanation(ConfidenceLevel confidence, ConfidenceFactors factors);
}

/// <summary>
/// Factors that influence confidence calculation.
/// </summary>
public class ConfidenceFactors
{
    /// <summary>
    /// Average peer review score (1-10).
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    /// Score variance (lower = more agreement).
    /// </summary>
    public double ScoreVariance { get; set; }

    /// <summary>
    /// Endorsement rate (0-1).
    /// </summary>
    public double EndorsementRate { get; set; }

    /// <summary>
    /// Number of supporting sources.
    /// </summary>
    public int SourceCount { get; set; }

    /// <summary>
    /// Number of agents that agree.
    /// </summary>
    public int AgentAgreementCount { get; set; }

    /// <summary>
    /// Whether Devil's Advocate endorsed.
    /// </summary>
    public bool DevilsAdvocateEndorsed { get; set; }
}

/// <summary>
/// Implementation of confidence calculator.
/// </summary>
public class ConfidenceCalculator : IConfidenceCalculator
{
    // Thresholds for confidence levels
    private const double HighConfidenceMinScore = 7.5;
    private const double MediumConfidenceMinScore = 5.5;
    private const double HighConfidenceMinEndorsement = 0.8;
    private const double MediumConfidenceMinEndorsement = 0.5;
    private const int HighConfidenceMinSources = 3;
    private const int MediumConfidenceMinSources = 1;

    /// <inheritdoc />
    public ConfidenceLevel CalculateFindingConfidence(
        IReadOnlyList<SubmissionScore> submissionScores,
        double endorsementRate,
        int sourceCount)
    {
        if (submissionScores.Count == 0)
            return ConfidenceLevel.Low;

        var avgScore = submissionScores.Average(s => s.AverageScore);
        var avgConfidence = submissionScores
            .Select(s => s.Confidence)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .First().Key;

        // Calculate composite confidence
        var scorePoints = GetScorePoints(avgScore);
        var endorsementPoints = GetEndorsementPoints(endorsementRate);
        var sourcePoints = GetSourcePoints(sourceCount);
        var consensusPoints = avgConfidence == ConfidenceLevel.High ? 2 : avgConfidence == ConfidenceLevel.Medium ? 1 : 0;

        var totalPoints = scorePoints + endorsementPoints + sourcePoints + consensusPoints;

        // Max points: 3 + 2 + 2 + 2 = 9
        return totalPoints switch
        {
            >= 7 => ConfidenceLevel.High,
            >= 4 => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };
    }

    /// <inheritdoc />
    public ConfidenceLevel CalculateOverallConfidence(
        IReadOnlyList<SubmissionScore> allScores,
        IReadOnlyList<PeerReview> peerReviews)
    {
        if (allScores.Count == 0)
            return ConfidenceLevel.Low;

        var avgScore = allScores.Average(s => s.AverageScore);
        var endorsementRate = peerReviews.Count > 0
            ? (double)peerReviews.Count(r => r.Endorses) / peerReviews.Count
            : 0;

        // Check for high consensus (low variance)
        var highConfidenceCount = allScores.Count(s => s.Confidence == ConfidenceLevel.High);
        var consensusRate = (double)highConfidenceCount / allScores.Count;

        // Overall confidence based on multiple factors
        if (avgScore >= HighConfidenceMinScore &&
            endorsementRate >= HighConfidenceMinEndorsement &&
            consensusRate >= 0.5)
        {
            return ConfidenceLevel.High;
        }

        if (avgScore >= MediumConfidenceMinScore &&
            endorsementRate >= MediumConfidenceMinEndorsement)
        {
            return ConfidenceLevel.Medium;
        }

        return ConfidenceLevel.Low;
    }

    /// <inheritdoc />
    public ConfidenceLevel ScoreToConfidence(double score)
    {
        return score switch
        {
            >= HighConfidenceMinScore => ConfidenceLevel.High,
            >= MediumConfidenceMinScore => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };
    }

    /// <inheritdoc />
    public string GetConfidenceExplanation(ConfidenceLevel confidence, ConfidenceFactors factors)
    {
        var explanations = new List<string>();

        switch (confidence)
        {
            case ConfidenceLevel.High:
                explanations.Add($"Strong peer review scores (avg: {factors.AverageScore:F1}/10)");
                if (factors.EndorsementRate >= HighConfidenceMinEndorsement)
                    explanations.Add($"High endorsement rate ({factors.EndorsementRate:P0})");
                if (factors.SourceCount >= HighConfidenceMinSources)
                    explanations.Add($"Well-sourced ({factors.SourceCount} sources)");
                if (factors.DevilsAdvocateEndorsed)
                    explanations.Add("Validated by critical analysis");
                break;

            case ConfidenceLevel.Medium:
                explanations.Add($"Moderate peer review scores (avg: {factors.AverageScore:F1}/10)");
                if (factors.ScoreVariance > 2)
                    explanations.Add("Some disagreement among reviewers");
                if (factors.SourceCount < HighConfidenceMinSources)
                    explanations.Add("Limited source coverage");
                break;

            case ConfidenceLevel.Low:
                if (factors.AverageScore < MediumConfidenceMinScore)
                    explanations.Add($"Lower peer review scores (avg: {factors.AverageScore:F1}/10)");
                if (factors.EndorsementRate < MediumConfidenceMinEndorsement)
                    explanations.Add($"Low endorsement rate ({factors.EndorsementRate:P0})");
                if (factors.ScoreVariance > 3)
                    explanations.Add("Significant disagreement among reviewers");
                if (factors.SourceCount == 0)
                    explanations.Add("No sources cited");
                break;
        }

        return explanations.Count > 0
            ? string.Join("; ", explanations)
            : $"{confidence} confidence";
    }

    private static int GetScorePoints(double score)
    {
        return score switch
        {
            >= 8.0 => 3,
            >= 7.0 => 2,
            >= 5.5 => 1,
            _ => 0
        };
    }

    private static int GetEndorsementPoints(double rate)
    {
        return rate switch
        {
            >= 0.9 => 2,
            >= 0.7 => 1,
            _ => 0
        };
    }

    private static int GetSourcePoints(int count)
    {
        return count switch
        {
            >= 5 => 2,
            >= 2 => 1,
            _ => 0
        };
    }
}
