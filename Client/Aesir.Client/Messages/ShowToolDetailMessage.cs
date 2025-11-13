using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of a tool in the system.
/// </summary>
public class ShowToolDetailMessage(AesirToolBase? tool)
{
    /// <summary>
    /// Represents a tool entity, encapsulated within the <see cref="ShowToolDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirToolBase"/> which provides core tool information such as
    /// ID, name, type, and description
    /// </summary>
    public AesirToolBase Tool { get; set; } = tool ?? new AesirToolBase();
}