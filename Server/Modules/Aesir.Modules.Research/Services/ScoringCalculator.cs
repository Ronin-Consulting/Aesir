using Aesir.Modules.Research.Models;
using ConfidenceLevel = Aesir.Modules.Research.Models.ConfidenceLevel;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Calculates weighted scores for peer reviews.
/// </summary>
public interface IScoringCalculator
{
    /// <summary>
    /// Calculates the weighted average score from individual criteria scores.
    /// </summary>
    /// <param name="depth">Score for research depth (1-10).</param>
    /// <param name="accuracy">Score for accuracy/correctness (1-10).</param>
    /// <param name="sourceQuality">Score for source quality (1-10).</param>
    /// <param name="novelty">Score for novelty/insight (1-10).</param>
    /// <param name="coherence">Score for coherence/clarity (1-10).</param>
    /// <returns>The weighted average score (1-10).</returns>
    double CalculateWeightedAverage(
        double depth,
        double accuracy,
        double sourceQuality,
        double novelty,
        double coherence);

    /// <summary>
    /// Calculates the aggregate score for a submission across all peer reviews.
    /// </summary>
    /// <param name="reviews">The peer reviews for the submission.</param>
    /// <returns>The aggregate score and statistics.</returns>
    SubmissionScore CalculateAggregateScore(IReadOnlyList<PeerReview> reviews);

    /// <summary>
    /// Calculates confidence level based on score distribution.
    /// </summary>
    /// <param name="scores">The individual review scores.</param>
    /// <returns>Confidence level (High, Medium, Low).</returns>
    ConfidenceLevel CalculateConfidence(IReadOnlyList<double> scores);

    /// <summary>
    /// Gets the weight for a specific criterion.
    /// </summary>
    /// <param name="criterion">The criterion name.</param>
    /// <returns>The weight (0-1).</returns>
    double GetCriterionWeight(string criterion);
}

/// <summary>
/// Aggregated score for a submission across all peer reviews.
/// </summary>
public class SubmissionScore
{
    /// <summary>
    /// The submission ID.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// Average score across all reviews.
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    /// Median score across all reviews.
    /// </summary>
    public double MedianScore { get; set; }

    /// <summary>
    /// Minimum score received.
    /// </summary>
    public double MinScore { get; set; }

    /// <summary>
    /// Maximum score received.
    /// </summary>
    public double MaxScore { get; set; }

    /// <summary>
    /// Standard deviation of scores.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Number of reviews included.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Number of endorsements received.
    /// </summary>
    public int EndorsementCount { get; set; }

    /// <summary>
    /// Confidence level based on score agreement.
    /// </summary>
    public ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Individual criterion averages.
    /// </summary>
    public Dictionary<string, double> CriterionAverages { get; set; } = new();
}

/// <summary>
/// Implementation of the scoring calculator with configurable weights.
/// </summary>
public class ScoringCalculator : IScoringCalculator
{
    // Default weights for each criterion (should sum to 1.0)
    private readonly Dictionary<string, double> _weights = new()
    {
        { "depth", 0.25 },
        { "accuracy", 0.25 },
        { "source_quality", 0.20 },
        { "novelty", 0.15 },
        { "coherence", 0.15 }
    };

    // Thresholds for confidence calculation
    private const double HighConfidenceMaxStdDev = 1.0;
    private const double MediumConfidenceMaxStdDev = 2.0;

    /// <inheritdoc />
    public double CalculateWeightedAverage(
        double depth,
        double accuracy,
        double sourceQuality,
        double novelty,
        double coherence)
    {
        // Clamp all values to 1-10 range
        depth = Math.Clamp(depth, 1, 10);
        accuracy = Math.Clamp(accuracy, 1, 10);
        sourceQuality = Math.Clamp(sourceQuality, 1, 10);
        novelty = Math.Clamp(novelty, 1, 10);
        coherence = Math.Clamp(coherence, 1, 10);

        var weightedSum =
            depth * _weights["depth"] +
            accuracy * _weights["accuracy"] +
            sourceQuality * _weights["source_quality"] +
            novelty * _weights["novelty"] +
            coherence * _weights["coherence"];

        return Math.Round(weightedSum, 2);
    }

    /// <inheritdoc />
    public SubmissionScore CalculateAggregateScore(IReadOnlyList<PeerReview> reviews)
    {
        if (reviews.Count == 0)
        {
            return new SubmissionScore
            {
                AverageScore = 0,
                MedianScore = 0,
                MinScore = 0,
                MaxScore = 0,
                StandardDeviation = 0,
                ReviewCount = 0,
                EndorsementCount = 0,
                Confidence = ConfidenceLevel.Low
            };
        }

        var scores = reviews.Select(r => r.WeightedAverage).ToList();
        var submissionId = reviews.First().SubmissionId;

        // Calculate statistics
        var average = scores.Average();
        var median = CalculateMedian(scores);
        var min = scores.Min();
        var max = scores.Max();
        var stdDev = CalculateStandardDeviation(scores, average);
        var endorsements = reviews.Count(r => r.Endorses);

        // Calculate criterion averages
        var criterionAverages = new Dictionary<string, double>
        {
            { "depth", reviews.Average(r => r.ScoreDepth) },
            { "accuracy", reviews.Average(r => r.ScoreAccuracy) },
            { "source_quality", reviews.Average(r => r.ScoreSourceQuality) },
            { "novelty", reviews.Average(r => r.ScoreNovelty) },
            { "coherence", reviews.Average(r => r.ScoreCoherence) }
        };

        return new SubmissionScore
        {
            SubmissionId = submissionId,
            AverageScore = Math.Round(average, 2),
            MedianScore = Math.Round(median, 2),
            MinScore = Math.Round(min, 2),
            MaxScore = Math.Round(max, 2),
            StandardDeviation = Math.Round(stdDev, 2),
            ReviewCount = reviews.Count,
            EndorsementCount = endorsements,
            Confidence = CalculateConfidence(scores),
            CriterionAverages = criterionAverages.ToDictionary(
                kv => kv.Key,
                kv => Math.Round(kv.Value, 2))
        };
    }

    /// <inheritdoc />
    public ConfidenceLevel CalculateConfidence(IReadOnlyList<double> scores)
    {
        if (scores.Count < 2)
            return ConfidenceLevel.Low;

        var stdDev = CalculateStandardDeviation(scores, scores.Average());

        return stdDev switch
        {
            <= HighConfidenceMaxStdDev => ConfidenceLevel.High,
            <= MediumConfidenceMaxStdDev => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };
    }

    /// <inheritdoc />
    public double GetCriterionWeight(string criterion)
    {
        return _weights.TryGetValue(criterion.ToLowerInvariant(), out var weight) ? weight : 0;
    }

    /// <summary>
    /// Calculates the median of a list of values.
    /// </summary>
    private static double CalculateMedian(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        }

        return sorted[count / 2];
    }

    /// <summary>
    /// Calculates the standard deviation.
    /// </summary>
    private static double CalculateStandardDeviation(IReadOnlyList<double> values, double mean)
    {
        if (values.Count < 2)
            return 0;

        var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }
}
