namespace Aesir.Tools.LegalValidator.Configuration;

/// <summary>
/// Configuration options for the Legal Validator.
/// </summary>
public class LegalValidatorOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "LegalValidator";

    /// <summary>
    /// AESIR API configuration.
    /// </summary>
    public AesirApiOptions AesirApi { get; set; } = new();

    /// <summary>
    /// Claude API configuration.
    /// </summary>
    public ClaudeOptions Claude { get; set; } = new();

    /// <summary>
    /// Validation run configuration.
    /// </summary>
    public ValidationOptions Validation { get; set; } = new();

    /// <summary>
    /// Question configuration.
    /// </summary>
    public QuestionOptions Questions { get; set; } = new();
}

/// <summary>
/// AESIR API connection options.
/// </summary>
public class AesirApiOptions
{
    /// <summary>
    /// Base URL for the AESIR API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://aesir.localhost";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Claude API options for evaluation.
/// </summary>
public class ClaudeOptions
{
    /// <summary>
    /// Anthropic API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Claude model to use for evaluation.
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-20250514";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Validation run options.
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Default concurrency for parallel requests.
    /// </summary>
    public int DefaultConcurrency { get; set; } = 3;

    /// <summary>
    /// Output directory for reports.
    /// </summary>
    public string OutputDirectory { get; set; } = "./validation_results";
}

/// <summary>
/// Question loading options.
/// </summary>
public class QuestionOptions
{
    /// <summary>
    /// Whether to use embedded questions.
    /// </summary>
    public bool UseEmbeddedQuestions { get; set; } = true;

    /// <summary>
    /// Path to external questions file (optional).
    /// </summary>
    public string? ExternalQuestionsPath { get; set; }

    /// <summary>
    /// Categories to include (null = all).
    /// </summary>
    public List<string>? Categories { get; set; }
}
