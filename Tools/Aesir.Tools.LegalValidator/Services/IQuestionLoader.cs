using Aesir.Tools.LegalValidator.Models;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Service for loading legal questions from various sources.
/// </summary>
public interface IQuestionLoader
{
    /// <summary>
    /// Loads questions from the configured source.
    /// </summary>
    /// <param name="categories">Optional category filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of legal questions.</returns>
    Task<IReadOnlyList<LegalQuestion>> LoadQuestionsAsync(
        IEnumerable<QuestionCategory>? categories = null,
        CancellationToken ct = default);

    /// <summary>
    /// Loads questions from an external JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="categories">Optional category filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of legal questions.</returns>
    Task<IReadOnlyList<LegalQuestion>> LoadQuestionsFromFileAsync(
        string filePath,
        IEnumerable<QuestionCategory>? categories = null,
        CancellationToken ct = default);
}
