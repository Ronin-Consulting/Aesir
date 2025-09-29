using System.Text.Json.Serialization;
using Aesir.Common.Models;

namespace Aesir.Api.Server.Models;

public class AesirKernelLog : AesirKernelLogBase
{
    [JsonPropertyName("details")]
    public new AesirKernelLogDetails Details { get; set; } = null!;
}