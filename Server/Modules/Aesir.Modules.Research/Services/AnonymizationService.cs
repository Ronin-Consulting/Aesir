using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for anonymizing research submissions before peer review.
/// </summary>
public interface IAnonymizationService
{
    /// <summary>
    /// Anonymizes a list of submissions, replacing agent identities with anonymous IDs.
    /// </summary>
    /// <param name="submissions">The submissions to anonymize.</param>
    /// <returns>Dictionary of anonymized ID to submission with anonymized content.</returns>
    Task<Dictionary<string, AnonymizedSubmission>> AnonymizeSubmissionsAsync(
        IReadOnlyList<ResearchSubmission> submissions);

    /// <summary>
    /// Gets the anonymized ID for a submission.
    /// </summary>
    /// <param name="submissionId">The original submission ID.</param>
    /// <returns>The anonymized ID (A, B, C, etc.).</returns>
    string GetAnonymizedId(Guid submissionId);

    /// <summary>
    /// Gets the original submission ID from an anonymized ID.
    /// </summary>
    /// <param name="anonymizedId">The anonymized ID.</param>
    /// <returns>The original submission ID, or null if not found.</returns>
    Guid? GetOriginalId(string anonymizedId);

    /// <summary>
    /// Clears the anonymization mapping.
    /// </summary>
    void ClearMapping();
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
/// Uses simple pattern replacement for anonymization.
/// </summary>
public class AnonymizationService : IAnonymizationService
{
    private readonly ILogger<AnonymizationService> _logger;
    private readonly Dictionary<Guid, string> _submissionToAnonymized = new();
    private readonly Dictionary<string, Guid> _anonymizedToSubmission = new();

    // Letters for anonymized IDs (up to 26 researchers)
    private static readonly string[] AnonymizedIds =
        Enumerable.Range(0, 26).Select(i => ((char)('A' + i)).ToString()).ToArray();

    public AnonymizationService(ILogger<AnonymizationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Dictionary<string, AnonymizedSubmission>> AnonymizeSubmissionsAsync(
        IReadOnlyList<ResearchSubmission> submissions)
    {
        _logger.LogInformation("Anonymizing {Count} submissions", submissions.Count);

        ClearMapping();
        var result = new Dictionary<string, AnonymizedSubmission>();

        // Shuffle submissions to randomize assignment
        var shuffled = submissions.OrderBy(_ => Guid.NewGuid()).ToList();

        for (var i = 0; i < shuffled.Count; i++)
        {
            var submission = shuffled[i];
            var anonymizedId = AnonymizedIds[i % AnonymizedIds.Length];

            // Store bidirectional mapping
            _submissionToAnonymized[submission.Id] = anonymizedId;
            _anonymizedToSubmission[anonymizedId] = submission.Id;

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

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public string GetAnonymizedId(Guid submissionId)
    {
        return _submissionToAnonymized.TryGetValue(submissionId, out var id) ? id : string.Empty;
    }

    /// <inheritdoc />
    public Guid? GetOriginalId(string anonymizedId)
    {
        return _anonymizedToSubmission.TryGetValue(anonymizedId, out var id) ? id : null;
    }

    /// <inheritdoc />
    public void ClearMapping()
    {
        _submissionToAnonymized.Clear();
        _anonymizedToSubmission.Clear();
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
