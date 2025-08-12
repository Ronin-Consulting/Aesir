using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message that is used to show the details of an agent in the system.
/// </summary>
public class ShowAgentDetailMessage(AesirAgentBase? agent)
{
    /// <summary>
    /// Represents an agent entity, encapsulated within the <see cref="ShowAgentDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirAgentBase"/> which provides core agent information such as
    /// ID, name, chat model, embedding model, vision model, source, and associated prompts.
    /// </summary>
    public AesirAgentBase Agent { get; set; } = agent ?? new AesirAgentBase();
}