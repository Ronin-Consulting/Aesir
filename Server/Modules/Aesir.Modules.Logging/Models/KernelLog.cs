using System.Text.Json.Serialization;
using Aesir.Common.Models;

namespace Aesir.Modules.Logging.Models;

/// <summary>
/// Represents a kernel execution log entry with detailed information.
/// Stored in aesir.aesir_log_kernel table.
/// </summary>
public class KernelLog : AesirKernelLogBase
{
    [JsonPropertyName("details")]
    public new KernelLogDetails Details { get; set; } = null!;
}