using System.Text.Json.Serialization;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Research agent roles in a research team.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchRole
{
    /// <summary>
    /// Exhaustive investigation specialist - digs deep into sources.
    /// </summary>
    DeepDiver,

    /// <summary>
    /// Pattern recognition specialist - connects disparate findings.
    /// </summary>
    Synthesizer,

    /// <summary>
    /// Critical analysis specialist - challenges assumptions and finds gaps.
    /// </summary>
    DevilsAdvocate,

    /// <summary>
    /// Evaluates and synthesizes final report - always uses larger model.
    /// </summary>
    Chairman
}

/// <summary>
/// Research mode determining depth and duration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchMode
{
    /// <summary>
    /// Quick mode: 2 agents only, no peer review. (Post-MVP)
    /// </summary>
    Quick,

    /// <summary>
    /// Standard mode: 3 agents + Chairman with peer review. (MVP)
    /// </summary>
    Standard,

    /// <summary>
    /// Deep mode: Multi-round research with gap filling. (Post-MVP)
    /// </summary>
    Deep
}

/// <summary>
/// Overall status of a research session.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchStatus
{
    /// <summary>
    /// Session created, not yet started.
    /// </summary>
    Created,

    /// <summary>
    /// Waiting for user to answer clarification questions.
    /// </summary>
    AwaitingClarification,

    /// <summary>
    /// Agents are creating research plans.
    /// </summary>
    Planning,

    /// <summary>
    /// Agents are executing research.
    /// </summary>
    Researching,

    /// <summary>
    /// Submissions are being anonymized for peer review.
    /// </summary>
    Anonymizing,

    /// <summary>
    /// Agents are reviewing each other's work.
    /// </summary>
    PeerReviewing,

    /// <summary>
    /// Chairman is synthesizing the final report.
    /// </summary>
    Synthesizing,

    /// <summary>
    /// Research completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Research failed due to an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Research was cancelled by the user.
    /// </summary>
    Cancelled
}

/// <summary>
/// Current phase of the research workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchPhase
{
    /// <summary>
    /// Generating clarification questions for the user.
    /// </summary>
    Clarification,

    /// <summary>
    /// Agents creating chain-of-thought research plans.
    /// </summary>
    Planning,

    /// <summary>
    /// Agents executing research with tools.
    /// </summary>
    Research,

    /// <summary>
    /// Anonymizing submissions for peer review.
    /// </summary>
    Anonymization,

    /// <summary>
    /// Cross-agent peer review with scoring.
    /// </summary>
    PeerReview,

    /// <summary>
    /// Chairman synthesizing final report.
    /// </summary>
    Synthesis
}

/// <summary>
/// Status of an individual agent's submission.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubmissionStatus
{
    /// <summary>
    /// Submission not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// Agent is creating the research plan.
    /// </summary>
    Planning,

    /// <summary>
    /// Agent is executing research.
    /// </summary>
    Researching,

    /// <summary>
    /// Submission completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Submission failed due to an error.
    /// </summary>
    Failed
}

/// <summary>
/// Type of event in the research trail audit log.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchTrailEventType
{
    /// <summary>
    /// Research session created.
    /// </summary>
    SessionCreated,

    /// <summary>
    /// Phase changed.
    /// </summary>
    PhaseChanged,

    /// <summary>
    /// Agent started working.
    /// </summary>
    AgentStarted,

    /// <summary>
    /// Agent completed work.
    /// </summary>
    AgentCompleted,

    /// <summary>
    /// Agent made a tool call (RAG, web search, etc.).
    /// </summary>
    ToolCall,

    /// <summary>
    /// RAG query executed.
    /// </summary>
    RagQuery,

    /// <summary>
    /// Web search executed.
    /// </summary>
    WebSearch,

    /// <summary>
    /// Peer review submitted.
    /// </summary>
    PeerReviewSubmitted,

    /// <summary>
    /// Report generated.
    /// </summary>
    ReportGenerated,

    /// <summary>
    /// Session completed.
    /// </summary>
    SessionCompleted,

    /// <summary>
    /// Error occurred.
    /// </summary>
    Error
}

/// <summary>
/// Confidence level for research findings.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConfidenceLevel
{
    /// <summary>
    /// Low confidence - limited evidence or contested findings.
    /// </summary>
    Low,

    /// <summary>
    /// Medium confidence - reasonable evidence with some caveats.
    /// </summary>
    Medium,

    /// <summary>
    /// High confidence - strong evidence and consensus among agents.
    /// </summary>
    High
}
