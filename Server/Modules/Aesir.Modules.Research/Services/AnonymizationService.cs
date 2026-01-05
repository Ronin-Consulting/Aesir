using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for anonymizing research submissions before peer review.
/// This service is stateless - all mappings are returned in the result.
/// </summary>
public interface IAnonymizationService
{
    /// <summary>
    /// Anonymizes a list of submissions, replacing agent identities with anonymous IDs.
    /// </summary>
    /// <param name="submissions">The submissions to anonymize.</param>
    /// <returns>Result containing anonymized submissions and bidirectional mappings.</returns>
    Task<AnonymizationResult> AnonymizeSubmissionsAsync(
        IReadOnlyList<ResearchSubmission> submissions);
}

/// <summary>
/// Result of the anonymization process, containing submissions and bidirectional mappings.
/// This class is immutable and thread-safe.
/// </summary>
public class AnonymizationResult
{
    /// <summary>
    /// Dictionary of anonymized ID (A, B, C...) to anonymized submission.
    /// </summary>
    public IReadOnlyDictionary<string, AnonymizedSubmission> Submissions { get; }

    /// <summary>
    /// Mapping from original submission ID to anonymized ID.
    /// </summary>
    public IReadOnlyDictionary<Guid, string> SubmissionToAnonymizedMap { get; }

    /// <summary>
    /// Mapping from anonymized ID to original submission ID.
    /// </summary>
    public IReadOnlyDictionary<string, Guid> AnonymizedToSubmissionMap { get; }

    public AnonymizationResult(
        Dictionary<string, AnonymizedSubmission> submissions,
        Dictionary<Guid, string> submissionToAnonymized,
        Dictionary<string, Guid> anonymizedToSubmission)
    {
        Submissions = submissions;
        SubmissionToAnonymizedMap = submissionToAnonymized;
        AnonymizedToSubmissionMap = anonymizedToSubmission;
    }

    /// <summary>
    /// Gets the anonymized ID for a submission.
    /// </summary>
    /// <param name="submissionId">The original submission ID.</param>
    /// <returns>The anonymized ID (A, B, C, etc.), or empty string if not found.</returns>
    public string GetAnonymizedId(Guid submissionId)
    {
        return SubmissionToAnonymizedMap.TryGetValue(submissionId, out var id) ? id : string.Empty;
    }

    /// <summary>
    /// Gets the original submission ID from an anonymized ID.
    /// </summary>
    /// <param name="anonymizedId">The anonymized ID.</param>
    /// <returns>The original submission ID, or null if not found.</returns>
    public Guid? GetOriginalId(string anonymizedId)
    {
        return AnonymizedToSubmissionMap.TryGetValue(anonymizedId, out var id) ? id : null;
    }
}

/// <summary>
/// Represents an anonymized submission for peer review.
/// </summary>
public class AnonymizedSubmission
{
    /// <summary>
    /// The anonymized identifier (A, B, C, etc.).
    /// </summary>
    public string AnonymizedId { get; set; } = string.Empty;

    /// <summary>
    /// The original submission ID (for de-anonymization after review).
    /// </summary>
    public Guid OriginalSubmissionId { get; set; }

    /// <summary>
    /// The original agent ID.
    /// </summary>
    public Guid OriginalAgentId { get; set; }

    /// <summary>
    /// The original role.
    /// </summary>
    public ResearchRole OriginalRole { get; set; }

    /// <summary>
    /// The anonymized content (role references removed).
    /// </summary>
    public string AnonymizedContent { get; set; } = string.Empty;

    /// <summary>
    /// The original plan (kept for reference, not shown to reviewers).
    /// </summary>
    public string? OriginalPlan { get; set; }

    /// <summary>
    /// The sources from the original submission.
    /// </summary>
    public List<ResearchSource>? Sources { get; set; }
}

/// <summary>
/// Implementation of the anonymization service.
/// This service is stateless - all mappings are returned in the result.
/// Uses simple pattern replacement for anonymization.
/// </summary>
public class AnonymizationService : IAnonymizationService
{
    private readonly ILogger<AnonymizationService> _logger;

    // Letters for anonymized IDs (up to 26 researchers)
    private static readonly string[] AnonymizedIds =
        Enumerable.Range(0, 26).Select(i => ((char)('A' + i)).ToString()).ToArray();

    public AnonymizationService(ILogger<AnonymizationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<AnonymizationResult> AnonymizeSubmissionsAsync(
        IReadOnlyList<ResearchSubmission> submissions)
    {
        _logger.LogInformation("Anonymizing {Count} submissions", submissions.Count);

        // All state is local to this method - no instance state
        var submissionToAnonymized = new Dictionary<Guid, string>();
        var anonymizedToSubmission = new Dictionary<string, Guid>();
        var result = new Dictionary<string, AnonymizedSubmission>();

        // Shuffle submissions to randomize assignment
        var shuffled = submissions.OrderBy(_ => Guid.NewGuid()).ToList();

        for (var i = 0; i < shuffled.Count; i++)
        {
            var submission = shuffled[i];
            var anonymizedId = AnonymizedIds[i % AnonymizedIds.Length];

            // Store bidirectional mapping (local to this result)
            submissionToAnonymized[submission.Id] = anonymizedId;
            anonymizedToSubmission[anonymizedId] = submission.Id;

            // Anonymize the content
            var anonymizedContent = AnonymizeContent(submission.Content, submission.Role);

            result[anonymizedId] = new AnonymizedSubmission
            {
                AnonymizedId = anonymizedId,
                OriginalSubmissionId = submission.Id,
                OriginalAgentId = submission.AgentId,
                OriginalRole = submission.Role,
                AnonymizedContent = anonymizedContent,
                OriginalPlan = submission.Plan,
                Sources = submission.Sources
            };

            _logger.LogDebug("Assigned {Role} submission {SubmissionId} -> {AnonymizedId}",
                submission.Role, submission.Id, anonymizedId);
        }

        _logger.LogInformation("Anonymization complete. Created {Count} anonymized submissions", result.Count);

        // Return immutable result containing all data and mappings
        return Task.FromResult(new AnonymizationResult(result, submissionToAnonymized, anonymizedToSubmission));
    }

    /// <summary>
    /// Anonymizes the content by removing role-specific references.
    /// </summary>
    private static string AnonymizeContent(string content, ResearchRole role)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var result = content;

        // Remove role-specific headers and self-references
        var rolePatterns = GetRolePatterns(role);
        foreach (var pattern in rolePatterns)
        {
            result = result.Replace(pattern, "Researcher", StringComparison.OrdinalIgnoreCase);
        }

        // Remove common self-reference patterns
        var selfReferences = new[]
        {
            "As a Deep Diver", "As the Deep Diver",
            "As a Synthesizer", "As the Synthesizer",
            "As Devil's Advocate", "As the Devil's Advocate",
            "As a Devil's Advocate",
            "From my perspective as",
            "In my role as",
            "My role as"
        };

        foreach (var reference in selfReferences)
        {
            result = result.Replace(reference, "As a researcher", StringComparison.OrdinalIgnoreCase);
        }

        // Replace role name headers
        result = result.Replace("# Deep Diver", "# Researcher", StringComparison.OrdinalIgnoreCase);
        result = result.Replace("# Synthesizer", "# Researcher", StringComparison.OrdinalIgnoreCase);
        result = result.Replace("# Devil's Advocate", "# Researcher", StringComparison.OrdinalIgnoreCase);

        return result;
    }

    /// <summary>
    /// Gets patterns to remove for a specific role.
    /// </summary>
    private static string[] GetRolePatterns(ResearchRole role)
    {
        return role switch
        {
            ResearchRole.DeepDiver => new[]
            {
                "Deep Diver", "DeepDiver", "deep diver", "deepdiver",
                "deep-diver", "DEEP DIVER"
            },
            ResearchRole.Synthesizer => new[]
            {
                "Synthesizer", "synthesizer", "SYNTHESIZER",
                "The Synthesizer", "a Synthesizer"
            },
            ResearchRole.DevilsAdvocate => new[]
            {
                "Devil's Advocate", "Devils Advocate", "devil's advocate",
                "devils advocate", "DEVIL'S ADVOCATE", "DevilsAdvocate"
            },
            ResearchRole.Chairman => new[]
            {
                "Chairman", "chairman", "CHAIRMAN",
                "The Chairman", "a Chairman"
            },
            _ => Array.Empty<string>()
        };
    }
}
