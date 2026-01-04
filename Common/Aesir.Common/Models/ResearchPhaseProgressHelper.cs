namespace Aesir.Common.Models;

/// <summary>
/// Shared helper for mapping research phase progress to overall progress.
/// Used by both server (ResearchProgressBroadcaster) and client (ResearchStateService)
/// to ensure consistent progress calculation across the application.
/// </summary>
public static class ResearchPhaseProgressHelper
{
    /// <summary>
    /// Phase weight ranges for mapping phase-local progress (0-100%) to overall progress (0-100%).
    /// Each tuple is (StartPercent, EndPercent) representing the overall progress range for that phase.
    /// </summary>
    /// <remarks>
    /// Phase weights are distributed based on typical phase duration:
    /// - Clarification: 10% (quick initial phase)
    /// - Planning: 10% (agent planning)
    /// - Research: 35% (longest phase - actual research work)
    /// - Anonymization: 5% (quick preparation for peer review)
    /// - PeerReview: 25% (significant agent collaboration)
    /// - Synthesis: 15% (final report generation)
    /// </remarks>
    public static readonly IReadOnlyDictionary<ResearchPhaseBase, (int Start, int End)> PhaseRanges =
        new Dictionary<ResearchPhaseBase, (int Start, int End)>
        {
            { ResearchPhaseBase.Clarification, (0, 10) },      // 10% of total
            { ResearchPhaseBase.Planning, (10, 20) },          // 10% of total
            { ResearchPhaseBase.Research, (20, 55) },          // 35% of total (longest phase)
            { ResearchPhaseBase.Anonymization, (55, 60) },     // 5% of total (quick)
            { ResearchPhaseBase.PeerReview, (60, 85) },        // 25% of total
            { ResearchPhaseBase.Synthesis, (85, 100) }         // 15% of total
        };

    /// <summary>
    /// Maps phase-local progress (0-100%) to overall progress (0-100%).
    /// This prevents progress bar resets when transitioning between phases.
    /// </summary>
    /// <param name="phase">The current research phase.</param>
    /// <param name="phaseLocalPercent">Progress within the current phase (0-100).</param>
    /// <returns>Overall research progress (0-100).</returns>
    /// <example>
    /// // Research phase at 50% local progress
    /// var overall = MapPhaseProgressToOverall(ResearchPhaseBase.Research, 50);
    /// // Returns: 20 + (0.5 * 35) = 37% overall
    /// </example>
    public static int MapPhaseProgressToOverall(ResearchPhaseBase phase, int phaseLocalPercent)
    {
        // Clamp phase-local progress to valid range
        var clampedLocal = Math.Clamp(phaseLocalPercent, 0, 100);

        // Get phase boundaries
        if (!PhaseRanges.TryGetValue(phase, out var range))
        {
            // Unknown phase - return 0% as safe default
            return 0;
        }

        var (start, end) = range;
        var rangeSize = end - start;

        // Interpolate: start + (localPercent / 100.0 * rangeSize)
        var overallPercent = start + (int)Math.Round(clampedLocal / 100.0 * rangeSize);

        // Ensure result is within valid range
        return Math.Clamp(overallPercent, 0, 100);
    }

    /// <summary>
    /// Maps phase-local progress to overall progress using the integer phase value.
    /// This overload allows the server to use its ResearchPhase enum (which has matching int values).
    /// </summary>
    /// <param name="phaseValue">The integer value of the phase enum (0-5).</param>
    /// <param name="phaseLocalPercent">Progress within the current phase (0-100).</param>
    /// <returns>Overall research progress (0-100).</returns>
    public static int MapPhaseProgressToOverall(int phaseValue, int phaseLocalPercent)
    {
        if (!Enum.IsDefined(typeof(ResearchPhaseBase), phaseValue))
        {
            return 0;
        }

        return MapPhaseProgressToOverall((ResearchPhaseBase)phaseValue, phaseLocalPercent);
    }

    /// <summary>
    /// Estimates overall progress percentage based on the current phase.
    /// Uses the midpoint of each phase range for a reasonable estimate.
    /// Useful for session restoration when exact progress is unknown.
    /// </summary>
    /// <param name="phase">The current research phase.</param>
    /// <returns>Estimated overall progress (0-100).</returns>
    public static int EstimateProgressFromPhase(ResearchPhaseBase phase)
    {
        if (!PhaseRanges.TryGetValue(phase, out var range))
        {
            return 0;
        }

        return (range.Start + range.End) / 2;
    }

    /// <summary>
    /// Estimates overall progress using the integer phase value.
    /// </summary>
    /// <param name="phaseValue">The integer value of the phase enum (0-5).</param>
    /// <returns>Estimated overall progress (0-100).</returns>
    public static int EstimateProgressFromPhase(int phaseValue)
    {
        if (!Enum.IsDefined(typeof(ResearchPhaseBase), phaseValue))
        {
            return 0;
        }

        return EstimateProgressFromPhase((ResearchPhaseBase)phaseValue);
    }
}
