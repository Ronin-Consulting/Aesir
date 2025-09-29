using Aesir.Common.Models;

namespace Aesir.Client.Models;

public class AesirKernelLog:AesirKernelLogBase
{
    public string CreatedAtDisplay=>CreatedAt.ToString("g");
}