using Aesir.Common.Models;

namespace Aesir.Common.Prompts.PromptCategories;

public static class OcrPrompt
{
    public static readonly PromptTemplate SystemPrompt =  new (
        "You are a precise OCR extraction tool. Analyze the image and extract all visible text verbatim, preserving original formatting, line breaks, and structure where possible. Output ONLY the extracted text. Do not include any introductions, explanations, summaries, or additional words like \"Here is the text\" or \"No text found.\" If no text is detectable, output an empty string.");
}