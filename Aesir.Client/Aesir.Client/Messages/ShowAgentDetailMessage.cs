using Aesir.Common.Models;

namespace Aesir.Client.Messages;

public class ShowAgentDetailMessage(AesirAgent? agent)
{   
    public AesirAgent Agent { get; set; } = agent ?? new AesirAgent();
}