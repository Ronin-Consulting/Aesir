using System.Reflection;
using System.Text.Json;
using Aesir.Tools.LegalValidator.Configuration;
using Aesir.Tools.LegalValidator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Implementation of question loading from embedded resources and external files.
/// </summary>
public class QuestionLoader : IQuestionLoader
{
    private readonly ILogger<QuestionLoader> _logger;
    private readonly LegalValidatorOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public QuestionLoader(
        ILogger<QuestionLoader> logger,
        IOptions<LegalValidatorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<LegalQuestion>> LoadQuestionsAsync(
        IEnumerable<QuestionCategory>? categories = null,
        CancellationToken ct = default)
    {
        List<LegalQuestion> questions;

        if (!string.IsNullOrEmpty(_options.Questions.ExternalQuestionsPath))
        {
            _logger.LogInformation("Loading questions from external file: {Path}",
                _options.Questions.ExternalQuestionsPath);
            questions = await LoadFromFileAsync(_options.Questions.ExternalQuestionsPath, ct)
                .ConfigureAwait(false);
        }
        else if (_options.Questions.UseEmbeddedQuestions)
        {
            _logger.LogInformation("Loading questions from embedded resource");
            questions = await LoadFromEmbeddedResourceAsync(ct).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException(
                "No question source configured. Set UseEmbeddedQuestions=true or provide ExternalQuestionsPath.");
        }

        if (categories != null)
        {
            var categorySet = categories.ToHashSet();
            questions = questions.Where(q => categorySet.Contains(q.Category)).ToList();
            _logger.LogInformation("Filtered to {Count} questions in categories: {Categories}",
                questions.Count, string.Join(", ", categorySet));
        }

        _logger.LogInformation("Loaded {Count} questions", questions.Count);
        return questions;
    }

    public async Task<IReadOnlyList<LegalQuestion>> LoadQuestionsFromFileAsync(
        string filePath,
        IEnumerable<QuestionCategory>? categories = null,
        CancellationToken ct = default)
    {
        var questions = await LoadFromFileAsync(filePath, ct).ConfigureAwait(false);

        if (categories != null)
        {
            var categorySet = categories.ToHashSet();
            questions = questions.Where(q => categorySet.Contains(q.Category)).ToList();
        }

        return questions;
    }

    private async Task<List<LegalQuestion>> LoadFromEmbeddedResourceAsync(CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Aesir.Tools.LegalValidator.Resources.legal_questions.json";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Available resources: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
        }

        var questions = await JsonSerializer.DeserializeAsync<List<LegalQuestion>>(
            stream, JsonOptions, ct).ConfigureAwait(false);

        return questions ?? [];
    }

    private async Task<List<LegalQuestion>> LoadFromFileAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Questions file not found: {filePath}");
        }

        await using var stream = File.OpenRead(filePath);
        var questions = await JsonSerializer.DeserializeAsync<List<LegalQuestion>>(
            stream, JsonOptions, ct).ConfigureAwait(false);

        return questions ?? [];
    }
}
