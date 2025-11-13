using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirKernelLogBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the log.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    [JsonPropertyName("level")]
    public KernelLogLevel Level { get; set; } = KernelLogLevel.Info;

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when the log was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }  = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the conversation containing the message history.
    /// </summary>
    [JsonPropertyName("details")]
    public AesirKernelLogDetailsBase Details { get; set; } = null!;

}

public enum KernelLogLevel
{
    Info,
    Warning,
    Error
}