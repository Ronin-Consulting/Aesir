using Aesir.Common.Models;

namespace Aesir.Common.Prompts;

/// <summary>
/// Provides predefined, reusable static prompt templates for various operations within the system.
/// </summary>
/// <remarks>
/// This static class contains constants for prompt templates that are intended for consistent usage across the application.
/// The prompts define structured instructions or text formats to be utilized by components that require standard input for processing.
/// </remarks>
public static class SystemPrompts
{
    /// <summary>
    /// A predefined prompt template designed to transform a follow-up question into a standalone question.
    /// </summary>
    /// <remarks>
    /// This prompt takes into account the previous conversation context, referred to as "chat history,"
    /// and a follow-up input question to generate a revised question that does not rely on prior context.
    /// It is primarily used in scenarios where isolated understanding of a user's intent is required.
    /// </remarks>
    public static readonly PromptTemplate CondensePrompt = new(@"
Given the following conversation between a user and an AI assistant and a follow up question from user,
rephrase the follow up question to be a standalone question.

Chat History:
{chat_history}
Follow Up Input: {question}
Standalone question:");
}