using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirMcpServerBase
{
    /// <summary>
    /// Gets or sets the id of the server
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the server
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the server
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the location of the server
    /// </summary>
    [JsonPropertyName("location")]
    public ServerLocation? Location { get; set; }

    /// <summary>
    /// Gets or sets the command of the local server
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the arguments of the local server
    /// </summary>
    [JsonPropertyName("arguments")]
    public IList<string> Arguments { get; set; }

    /// <summary>
    /// Gets or sets the environment variables of the local server
    /// </summary>
    [JsonPropertyName("environment_variables")]
    public IDictionary<string, string?> EnvironmentVariables { get; set; }

    /// <summary>
    /// Gets or sets the URL of the remote server
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the HTTP headers of the remote server
    /// </summary>
    [JsonPropertyName("http_headers")]
    public IDictionary<string, string?> HttpHeaders { get; set; }
}

public enum ServerLocation
{
    [Description("Local")]
    Local,
    [Description("Remote")]
    Remote
}