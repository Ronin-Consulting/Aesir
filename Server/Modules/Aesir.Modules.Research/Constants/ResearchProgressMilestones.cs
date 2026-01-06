namespace Aesir.Modules.Research.Constants;

/// <summary>
/// Defines standard progress milestones for research phases.
/// These values represent phase-local progress (0-100% within each phase)
/// and are mapped to overall progress by ResearchPhaseProgressHelper.
/// </summary>
/// <remarks>
/// Using named constants ensures:
/// - Consistent progress reporting across all services
/// - Self-documenting code that explains what each milestone represents
/// - Easy adjustment of progress values in one central location
/// </remarks>
public static class ResearchProgressMilestones
{
    /// <summary>
    /// Phase has just started (0%).
    /// Used when entering a new phase before any work begins.
    /// </summary>
    public const int PhaseStart = 0;

    /// <summary>
    /// Phase is initializing (10%).
    /// Used when setting up resources, loading configurations, or preparing inputs.
    /// </summary>
    public const int PhaseInitializing = 10;

    /// <summary>
    /// Prompt has been built and is ready for LLM call (30%).
    /// Used after constructing the request but before sending to inference.
    /// </summary>
    public const int PromptBuilt = 30;

    /// <summary>
    /// LLM call has started (40%).
    /// Used when the inference request has been sent and we're awaiting response.
    /// </summary>
    public const int LlmCallStarted = 40;

    /// <summary>
    /// LLM is actively processing (50%).
    /// Used during streaming or when we know the LLM is working.
    /// </summary>
    public const int LlmProcessing = 50;

    /// <summary>
    /// LLM call has completed (85%).
    /// Used when the inference response has been received but before post-processing.
    /// </summary>
    public const int LlmCallCompleted = 85;

    /// <summary>
    /// Phase has completed (100%).
    /// Used when all work in the phase is done and ready to transition.
    /// </summary>
    public const int PhaseComplete = 100;
}
