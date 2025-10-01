using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of a log in the system.
/// </summary>
public class ShowLogDetailMessage(AesirKernelLog? log)
{   
    public AesirKernelLog Log  { get; set; } = log ?? new AesirKernelLog();
}
