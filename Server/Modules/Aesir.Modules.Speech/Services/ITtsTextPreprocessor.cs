namespace Aesir.Modules.Speech.Services;

/// <summary>
/// Preprocesses text for TTS to produce natural-sounding speech.
/// Converts markdown to plain text with contextual phrasing for lists,
/// code blocks, and other markdown elements.
/// </summary>
public interface ITtsTextPreprocessor
{
    /// <summary>
    /// Converts markdown text to natural speech text.
    /// </summary>
    /// <param name="markdownText">The input text potentially containing markdown formatting.</param>
    /// <returns>Plain text optimized for natural-sounding TTS output.</returns>
    /// <remarks>
    /// Transformations include:
    /// - Lists: Converted to natural speech (e.g., "Here are three items: A, B, and C.")
    /// - Code blocks: Replaced with "I've included a code example."
    /// - Inline code: Backticks stripped, content preserved
    /// - Links: Link text preserved, URLs removed
    /// - Emphasis/Bold: Markers stripped, content preserved
    /// - Headings: Text preserved with proper punctuation for pauses
    /// - Horizontal rules: Removed entirely
    /// </remarks>
    string PreprocessForSpeech(string markdownText);
}
